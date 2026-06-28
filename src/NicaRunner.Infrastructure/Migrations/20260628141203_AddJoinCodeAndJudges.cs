using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NicaRunner.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddJoinCodeAndJudges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "JoinCode",
                table: "Races",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "RaceStartUtc",
                table: "Races",
                type: "TEXT",
                nullable: true);

            // Asignar JoinCodes únicos a las filas existentes antes de crear el
            // índice único. La función para generar 6 chars hex aleatorios es
            // distinta por motor: Sqlite usa hex(randomblob(3)), Postgres usa
            // md5(random()::text). Detectado en duro: la primera versión de esta
            // migración solo tenía la rama Sqlite y rompía el deploy a Postgres
            // (relation/función inexistente) — verificado corriendo esta
            // migración contra un Postgres real antes de desplegar.
            if (migrationBuilder.ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                migrationBuilder.Sql("UPDATE \"Races\" SET \"JoinCode\" = upper(substr(md5(random()::text), 1, 6)) WHERE \"JoinCode\" = '';");
            }
            else
            {
                migrationBuilder.Sql("UPDATE Races SET JoinCode = upper(hex(randomblob(3))) WHERE JoinCode = '';");
            }

            migrationBuilder.CreateTable(
                name: "RaceJudges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RaceId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaceJudges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RaceJudges_Races_RaceId",
                        column: x => x.RaceId,
                        principalTable: "Races",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RaceJudges_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Races_JoinCode",
                table: "Races",
                column: "JoinCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RaceJudges_RaceId_UserId",
                table: "RaceJudges",
                columns: new[] { "RaceId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RaceJudges_UserId",
                table: "RaceJudges",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RaceJudges");

            migrationBuilder.DropIndex(
                name: "IX_Races_JoinCode",
                table: "Races");

            migrationBuilder.DropColumn(
                name: "JoinCode",
                table: "Races");

            migrationBuilder.DropColumn(
                name: "RaceStartUtc",
                table: "Races");
        }
    }
}
