using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NicaRunner.Infrastructure.Migrations
{
    /// <summary>
    /// public-runner-registration-manual-payment (Phase 1): creates Registrations,
    /// RegistrationLinks, and ReservedDorsals; adds RaceCategories.Capacidad/Precio/
    /// ConfirmedCount, Races.InscripcionesAbiertas/FechaLimiteInscripcion, and
    /// Runners.DorsalNormalizado + its additive unique index.
    ///
    /// design.md D11: Runners.DorsalNormalizado is a STAGED change within this single
    /// migration, in this exact order, because step 3 is the collision detector:
    ///   1. add the column NULLABLE
    ///   2. backfill every existing row (portable SQL below, mirrors DorsalNormalizer.Normalize)
    ///   3. ALTER the column to NOT NULL, then CREATE the additive UNIQUE INDEX
    /// If any race already has two runners whose dorsals normalize equally (e.g. "101"
    /// and "0101"), step 3 fails the deployment loudly instead of silently allowing the
    /// duplicate to persist. Run scripts/preflight-dorsal-duplicate-check.sql against
    /// production BEFORE deploying this migration and have an operator re-number any
    /// reported collision by hand — there is no safe automatic winner (tasks.md 1.11).
    /// </summary>
    public partial class AddRegistrationsAndDorsalNormalization : Migration
    {
        // Real bib numbers are short; capping the portable trailing-digit scan at this
        // depth keeps the generated SQL bounded while comfortably covering every
        // realistic Dorsal value. The pre-flight check (1.11) is the safety net for
        // anything this backfill heuristic cannot perfectly reconstruct.
        private const int MaxDorsalScanDepth = 18;

        private static string CharFromEnd(string s, int posFromEnd) =>
            $"SUBSTR({s}, LENGTH({s}) - {posFromEnd - 1}, 1)";

        // Nested CASE computing the length of the maximal trailing run of ASCII digits
        // in `s`, capped at MaxDorsalScanDepth. Portable across Sqlite and Postgres —
        // only LENGTH/SUBSTR/BETWEEN are used, no REGEXP/GLOB.
        private static string BuildTrailingDigitCountExpr(string s, int maxDepth)
        {
            var expr = maxDepth.ToString();
            for (var k = maxDepth - 1; k >= 1; k--)
            {
                expr = $"CASE WHEN LENGTH({s}) >= {k + 1} AND {CharFromEnd(s, k + 1)} BETWEEN '0' AND '9' " +
                       $"THEN ({expr}) ELSE {k} END";
            }

            return $"CASE WHEN LENGTH({s}) >= 1 AND {CharFromEnd(s, 1)} BETWEEN '0' AND '9' " +
                   $"THEN ({expr}) ELSE 0 END";
        }

        // Strips every character in [A-Z0-9] from `s` via nested REPLACE; LENGTH(...) = 0
        // afterwards iff `s` consists ONLY of chars in [A-Z0-9] — the same charset
        // DorsalNormalizer's regex requires for the whole string to split into
        // prefix+digits. Anything else (e.g. "VIP-1", with '-') must NOT have its
        // digits stripped, same passthrough rule as the C# implementation.
        private static string BuildAllowedCharsStrippedExpr(string s)
        {
            var expr = s;
            foreach (var c in "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ")
                expr = $"REPLACE({expr}, '{c}', '')";
            return expr;
        }

        // Portable equivalent of DorsalNormalizer.Normalize for the backfill UPDATE:
        // "21K007" -> "21K7"; "0101" -> "101"; "5K1234" -> "5K1234"; "VIP-1" -> "VIP-1".
        private static string BuildNormalizedDorsalExpr()
        {
            const string s = "UPPER(TRIM(\"Dorsal\"))";
            var digitCount = BuildTrailingDigitCountExpr(s, MaxDorsalScanDepth);
            var strippedAllowed = BuildAllowedCharsStrippedExpr(s);
            var prefix = $"SUBSTR({s}, 1, LENGTH({s}) - ({digitCount}))";
            var digitsPart = $"SUBSTR({s}, LENGTH({s}) - ({digitCount}) + 1)";

            return $"CASE WHEN LENGTH({strippedAllowed}) = 0 AND ({digitCount}) > 0 " +
                   $"THEN {prefix} || CAST(CAST({digitsPart} AS BIGINT) AS TEXT) " +
                   $"ELSE {s} END";
        }

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // --- RaceCategory: capacity/price home (D1) ---
            migrationBuilder.AddColumn<int>(
                name: "Capacidad",
                table: "RaceCategories",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Precio",
                table: "RaceCategories",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ConfirmedCount",
                table: "RaceCategories",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            // --- Race: explicit "inscripciones abiertas" gate + deadline ---
            migrationBuilder.AddColumn<bool>(
                name: "InscripcionesAbiertas",
                table: "Races",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaLimiteInscripcion",
                table: "Races",
                type: "TEXT",
                nullable: true);

            // --- Runner.DorsalNormalizado (D11) — staged: nullable first ---
            migrationBuilder.AddColumn<string>(
                name: "DorsalNormalizado",
                table: "Runners",
                type: "TEXT",
                nullable: true);

            // Step 2: backfill every existing row before the column is required or the
            // unique index is built.
            migrationBuilder.Sql(
                $"UPDATE \"Runners\" SET \"DorsalNormalizado\" = {BuildNormalizedDorsalExpr()} " +
                "WHERE \"DorsalNormalizado\" IS NULL;");

            // Step 3a: now that every row is populated, require the column.
            migrationBuilder.AlterColumn<string>(
                name: "DorsalNormalizado",
                table: "Runners",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            // Step 3b: the collision detector — additive, alongside the existing textual
            // IX_Runners_RaceId_Dorsal index (unchanged).
            migrationBuilder.CreateIndex(
                name: "IX_Runners_RaceId_DorsalNormalizado",
                table: "Runners",
                columns: new[] { "RaceId", "DorsalNormalizado" },
                unique: true);

            // --- Registrations ---
            migrationBuilder.CreateTable(
                name: "Registrations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RaceId = table.Column<int>(type: "INTEGER", nullable: false),
                    RaceCategoryId = table.Column<int>(type: "INTEGER", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", nullable: false),
                    Apellidos = table.Column<string>(type: "TEXT", nullable: true),
                    Telefono = table.Column<string>(type: "TEXT", nullable: true),
                    Email = table.Column<string>(type: "TEXT", nullable: true),
                    Sexo = table.Column<int>(type: "INTEGER", nullable: true),
                    Club = table.Column<string>(type: "TEXT", nullable: true),
                    FechaNacimiento = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ContactoEmergenciaNombre = table.Column<string>(type: "TEXT", nullable: true),
                    ContactoEmergenciaTelefono = table.Column<string>(type: "TEXT", nullable: true),
                    Estado = table.Column<int>(type: "INTEGER", nullable: false),
                    ReferenciaTransferencia = table.Column<string>(type: "TEXT", nullable: true),
                    PrecioAplicado = table.Column<decimal>(type: "TEXT", nullable: true),
                    RunnerId = table.Column<int>(type: "INTEGER", nullable: true),
                    RevisadoPorUserId = table.Column<int>(type: "INTEGER", nullable: true),
                    RevisadoAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    MotivoRechazo = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Registrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Registrations_Races_RaceId",
                        column: x => x.RaceId,
                        principalTable: "Races",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Registrations_RaceCategories_RaceCategoryId",
                        column: x => x.RaceCategoryId,
                        principalTable: "RaceCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Registrations_Runners_RunnerId",
                        column: x => x.RunnerId,
                        principalTable: "Runners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Registrations_Users_RevisadoPorUserId",
                        column: x => x.RevisadoPorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Registrations_RaceId_Estado",
                table: "Registrations",
                columns: new[] { "RaceId", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_Registrations_RaceCategoryId",
                table: "Registrations",
                column: "RaceCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Registrations_RunnerId",
                table: "Registrations",
                column: "RunnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Registrations_RevisadoPorUserId",
                table: "Registrations",
                column: "RevisadoPorUserId");

            // --- RegistrationLinks (mirrors PublicResultTokens, D4) ---
            migrationBuilder.CreateTable(
                name: "RegistrationLinks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RaceId = table.Column<int>(type: "INTEGER", nullable: false),
                    Token = table.Column<string>(type: "TEXT", nullable: false),
                    FechaExpiracion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsExpired = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrationLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegistrationLinks_Races_RaceId",
                        column: x => x.RaceId,
                        principalTable: "Races",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RegistrationLinks_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationLinks_Token",
                table: "RegistrationLinks",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationLinks_RaceId",
                table: "RegistrationLinks",
                column: "RaceId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationLinks_CreatedBy",
                table: "RegistrationLinks",
                column: "CreatedBy");

            // --- ReservedDorsals (D8/D9/D11) ---
            migrationBuilder.CreateTable(
                name: "ReservedDorsals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RaceId = table.Column<int>(type: "INTEGER", nullable: false),
                    Dorsal = table.Column<string>(type: "TEXT", nullable: false),
                    DorsalNormalizado = table.Column<string>(type: "TEXT", nullable: false),
                    Motivo = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedBy = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReservedDorsals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReservedDorsals_Races_RaceId",
                        column: x => x.RaceId,
                        principalTable: "Races",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReservedDorsals_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReservedDorsals_RaceId_DorsalNormalizado",
                table: "ReservedDorsals",
                columns: new[] { "RaceId", "DorsalNormalizado" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReservedDorsals_CreatedBy",
                table: "ReservedDorsals",
                column: "CreatedBy");

            // --- Postgres provider guard ---
            // Same origin as FixPostgresIdentityColumns/FixPostgresDecimalColumns/
            // FixPostgresBooleanColumns/FixPostgresDateTimeColumns: this migration is
            // scaffolded with Sqlite active, so the "INTEGER"/"TEXT" type annotations
            // above reach Postgres literally and would leave new Id columns without
            // identity generation, and new decimal/bool/DateTime columns unreadable as
            // their C# types. Same fix pattern as AddCategoryCatalogAndRunnerDemographics
            // (also a brand-new-tables-in-one-migration case): inline guard, no separate
            // Fix migration needed since these columns/tables did not exist before this PR.
            if (migrationBuilder.ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                foreach (var table in new[] { "Registrations", "RegistrationLinks", "ReservedDorsals" })
                {
                    migrationBuilder.Sql($"ALTER TABLE \"{table}\" ALTER COLUMN \"Id\" ADD GENERATED BY DEFAULT AS IDENTITY;");
                    migrationBuilder.Sql(
                        $"SELECT setval(pg_get_serial_sequence('\"{table}\"', 'Id'), " +
                        $"COALESCE((SELECT MAX(\"Id\") FROM \"{table}\"), 0) + 1, false);");
                }

                foreach (var (table, column) in new (string, string)[]
                {
                    ("RaceCategories", "Precio"),
                    ("Registrations", "PrecioAplicado")
                })
                {
                    migrationBuilder.Sql(
                        $"ALTER TABLE \"{table}\" ALTER COLUMN \"{column}\" TYPE numeric USING \"{column}\"::numeric;");
                }

                foreach (var (table, column) in new (string, string)[]
                {
                    ("Races", "InscripcionesAbiertas"),
                    ("RegistrationLinks", "IsExpired")
                })
                {
                    migrationBuilder.Sql(
                        $"ALTER TABLE \"{table}\" ALTER COLUMN \"{column}\" TYPE boolean " +
                        $"USING CASE WHEN \"{column}\" = 0 THEN false ELSE true END;");
                }

                foreach (var (table, column) in new (string, string)[]
                {
                    ("Races", "FechaLimiteInscripcion"),
                    ("Registrations", "FechaNacimiento"),
                    ("Registrations", "RevisadoAt"),
                    ("Registrations", "CreatedAt"),
                    ("RegistrationLinks", "FechaExpiracion"),
                    ("RegistrationLinks", "CreatedAt"),
                    ("ReservedDorsals", "CreatedAt")
                })
                {
                    migrationBuilder.Sql(
                        $"ALTER TABLE \"{table}\" ALTER COLUMN \"{column}\" TYPE timestamp without time zone " +
                        $"USING \"{column}\"::timestamp without time zone;");
                }
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ReservedDorsals");
            migrationBuilder.DropTable(name: "RegistrationLinks");
            migrationBuilder.DropTable(name: "Registrations");

            migrationBuilder.DropIndex(
                name: "IX_Runners_RaceId_DorsalNormalizado",
                table: "Runners");

            migrationBuilder.DropColumn(
                name: "DorsalNormalizado",
                table: "Runners");

            migrationBuilder.DropColumn(
                name: "InscripcionesAbiertas",
                table: "Races");

            migrationBuilder.DropColumn(
                name: "FechaLimiteInscripcion",
                table: "Races");

            migrationBuilder.DropColumn(
                name: "Capacidad",
                table: "RaceCategories");

            migrationBuilder.DropColumn(
                name: "Precio",
                table: "RaceCategories");

            migrationBuilder.DropColumn(
                name: "ConfirmedCount",
                table: "RaceCategories");
        }
    }
}
