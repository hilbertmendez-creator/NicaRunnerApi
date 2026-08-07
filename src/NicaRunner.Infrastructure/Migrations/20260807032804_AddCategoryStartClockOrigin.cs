using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NicaRunner.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryStartClockOrigin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StartIdempotencyKey",
                table: "RaceCategories",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StartOffsetConfianzaMs",
                table: "RaceCategories",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StartOrigen",
                table: "RaceCategories",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StartIdempotencyKey",
                table: "RaceCategories");

            migrationBuilder.DropColumn(
                name: "StartOffsetConfianzaMs",
                table: "RaceCategories");

            migrationBuilder.DropColumn(
                name: "StartOrigen",
                table: "RaceCategories");
        }
    }
}
