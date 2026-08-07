using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NicaRunner.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddResultDisputeState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ResultAudits_Users_AdminId",
                table: "ResultAudits");

            migrationBuilder.RenameColumn(
                name: "AdminId",
                table: "ResultAudits",
                newName: "ActorUserId");

            migrationBuilder.RenameIndex(
                name: "IX_ResultAudits_AdminId",
                table: "ResultAudits",
                newName: "IX_ResultAudits_ActorUserId");

            migrationBuilder.AddColumn<int>(
                name: "DisputeGroupId",
                table: "Results",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DisputeMotivo",
                table: "Results",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DorsalPropuesto",
                table: "Results",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Estado",
                table: "Results",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Valido");

            migrationBuilder.CreateIndex(
                name: "IX_Results_DisputeGroupId",
                table: "Results",
                column: "DisputeGroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_ResultAudits_Users_ActorUserId",
                table: "ResultAudits",
                column: "ActorUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Results_Results_DisputeGroupId",
                table: "Results",
                column: "DisputeGroupId",
                principalTable: "Results",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ResultAudits_Users_ActorUserId",
                table: "ResultAudits");

            migrationBuilder.DropForeignKey(
                name: "FK_Results_Results_DisputeGroupId",
                table: "Results");

            migrationBuilder.DropIndex(
                name: "IX_Results_DisputeGroupId",
                table: "Results");

            migrationBuilder.DropColumn(
                name: "DisputeGroupId",
                table: "Results");

            migrationBuilder.DropColumn(
                name: "DisputeMotivo",
                table: "Results");

            migrationBuilder.DropColumn(
                name: "DorsalPropuesto",
                table: "Results");

            migrationBuilder.DropColumn(
                name: "Estado",
                table: "Results");

            migrationBuilder.RenameColumn(
                name: "ActorUserId",
                table: "ResultAudits",
                newName: "AdminId");

            migrationBuilder.RenameIndex(
                name: "IX_ResultAudits_ActorUserId",
                table: "ResultAudits",
                newName: "IX_ResultAudits_AdminId");

            migrationBuilder.AddForeignKey(
                name: "FK_ResultAudits_Users_AdminId",
                table: "ResultAudits",
                column: "AdminId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
