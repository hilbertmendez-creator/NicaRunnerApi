using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NicaRunner.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRunnerPublicShareKeyAndRaceSlogan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PublicShareKey",
                table: "Runners",
                type: "TEXT",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Slogan",
                table: "Races",
                type: "TEXT",
                maxLength: 160,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Runners_PublicShareKey",
                table: "Runners",
                column: "PublicShareKey",
                unique: true,
                filter: "\"PublicShareKey\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Results_RaceId_CategoryId_TiempoLlegada",
                table: "Results",
                columns: new[] { "RaceId", "CategoryId", "TiempoLlegada" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Runners_PublicShareKey",
                table: "Runners");

            migrationBuilder.DropIndex(
                name: "IX_Results_RaceId_CategoryId_TiempoLlegada",
                table: "Results");

            migrationBuilder.DropColumn(
                name: "PublicShareKey",
                table: "Runners");

            migrationBuilder.DropColumn(
                name: "Slogan",
                table: "Races");
        }
    }
}
