using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NicaRunner.Infrastructure.Migrations
{
    /// <summary>
    /// Mismo origen que FixPostgresIdentityColumns/FixPostgresBooleanColumns:
    /// las migraciones anteriores se generaron con Sqlite activo, que
    /// almacena DateTime como texto ISO-8601 — la anotación de tipo "TEXT"
    /// quedó grabada literalmente y también es válida en Postgres como
    /// nombre de columna (texto plano), así que el ALTER/CREATE no fallaba,
    /// pero Npgsql no puede leer una columna "text" como DateTime: lanza
    /// InvalidCastException en cualquier SELECT (login, listar carreras,
    /// etc.). Detectado probando login real contra un Postgres real antes
    /// de desplegar — esto habría roto CADA endpoint que lee una entidad
    /// con un campo DateTime, no solo el login.
    /// </summary>
    public partial class FixPostgresDateTimeColumns : Migration
    {
        private static readonly (string Table, string Column)[] DateTimeColumns =
        {
            ("Users", "CreatedAt"),
            ("Races", "FechaCarrera"),
            ("Races", "RaceStartUtc"),
            ("Races", "CreatedAt"),
            ("Races", "UpdatedAt"),
            ("RaceJudges", "JoinedAt"),
            ("Runners", "CreatedAt"),
            ("Results", "TiempoLlegada"),
            ("Results", "CreatedAt"),
            ("Results", "UpdatedAt"),
            ("ResultAudits", "CreatedAt"),
            ("PublicResultTokens", "FechaExpiracion"),
            ("PublicResultTokens", "CreatedAt"),
            ("NotificationLogs", "CreatedAt"),
            ("NotificationLogs", "SentAt")
        };

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider != "Npgsql.EntityFrameworkCore.PostgreSQL")
                return;

            foreach (var (table, column) in DateTimeColumns)
            {
                migrationBuilder.Sql(
                    $"ALTER TABLE \"{table}\" ALTER COLUMN \"{column}\" TYPE timestamp without time zone " +
                    $"USING \"{column}\"::timestamp without time zone;");
            }
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider != "Npgsql.EntityFrameworkCore.PostgreSQL")
                return;

            foreach (var (table, column) in DateTimeColumns)
            {
                migrationBuilder.Sql($"ALTER TABLE \"{table}\" ALTER COLUMN \"{column}\" TYPE text USING \"{column}\"::text;");
            }
        }
    }
}
