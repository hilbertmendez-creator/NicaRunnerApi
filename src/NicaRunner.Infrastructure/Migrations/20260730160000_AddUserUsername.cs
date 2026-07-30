using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NicaRunner.Infrastructure.Migrations
{
    /// <summary>
    /// M1 (design.md §3.2, PR1): agrega Username (alias) nullable con índice único
    /// filtrado -- mismo patrón que GoogleId -- más las 3 columnas de lockout. Puramente
    /// aditiva: revertir es simplemente borrar estas 4 columnas y el índice, sin tocar
    /// datos existentes (tasks.md PR1, "rollback boundary: additive only").
    /// </summary>
    public partial class AddUserUsername : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Username",
                table: "Users",
                type: "TEXT",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FailedLoginCount",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockedUntilUtc",
                table: "Users",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastFailedLoginUtc",
                table: "Users",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true,
                filter: "\"Username\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_Username",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Username",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "FailedLoginCount",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LockedUntilUtc",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastFailedLoginUtc",
                table: "Users");
        }
    }
}
