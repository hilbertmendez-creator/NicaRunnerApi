using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NicaRunner.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixPublicResultTokenCreatorFk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PublicResultTokens_Users_CreatorId",
                table: "PublicResultTokens");

            migrationBuilder.DropIndex(
                name: "IX_PublicResultTokens_CreatorId",
                table: "PublicResultTokens");

            migrationBuilder.DropColumn(
                name: "CreatorId",
                table: "PublicResultTokens");

            migrationBuilder.CreateIndex(
                name: "IX_PublicResultTokens_CreatedBy",
                table: "PublicResultTokens",
                column: "CreatedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_PublicResultTokens_Users_CreatedBy",
                table: "PublicResultTokens",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PublicResultTokens_Users_CreatedBy",
                table: "PublicResultTokens");

            migrationBuilder.DropIndex(
                name: "IX_PublicResultTokens_CreatedBy",
                table: "PublicResultTokens");

            migrationBuilder.AddColumn<int>(
                name: "CreatorId",
                table: "PublicResultTokens",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_PublicResultTokens_CreatorId",
                table: "PublicResultTokens",
                column: "CreatorId");

            migrationBuilder.AddForeignKey(
                name: "FK_PublicResultTokens_Users_CreatorId",
                table: "PublicResultTokens",
                column: "CreatorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
