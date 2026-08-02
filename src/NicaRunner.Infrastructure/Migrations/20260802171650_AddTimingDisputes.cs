using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NicaRunner.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTimingDisputes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TimingDisputes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RaceId = table.Column<int>(type: "INTEGER", nullable: false),
                    ResultId = table.Column<int>(type: "INTEGER", nullable: true),
                    RunnerId = table.Column<int>(type: "INTEGER", nullable: true),
                    Dorsal = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    CorredorNombre = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ChipSeconds = table.Column<double>(type: "REAL", nullable: true),
                    CheckpointSeconds = table.Column<double>(type: "REAL", nullable: true),
                    CameraSeconds = table.Column<double>(type: "REAL", nullable: true),
                    Estado = table.Column<int>(type: "INTEGER", nullable: false),
                    CapturistaNombre = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    CapturaNote = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ResolvedByUserId = table.Column<int>(type: "INTEGER", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimingDisputes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TimingDisputes_Races_RaceId",
                        column: x => x.RaceId,
                        principalTable: "Races",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TimingDisputes_Results_ResultId",
                        column: x => x.ResultId,
                        principalTable: "Results",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TimingDisputes_Runners_RunnerId",
                        column: x => x.RunnerId,
                        principalTable: "Runners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TimingDisputes_Users_ResolvedByUserId",
                        column: x => x.ResolvedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TimingDisputes_RaceId_Estado",
                table: "TimingDisputes",
                columns: new[] { "RaceId", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_TimingDisputes_ResolvedByUserId",
                table: "TimingDisputes",
                column: "ResolvedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TimingDisputes_ResultId",
                table: "TimingDisputes",
                column: "ResultId");

            migrationBuilder.CreateIndex(
                name: "IX_TimingDisputes_RunnerId",
                table: "TimingDisputes",
                column: "RunnerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TimingDisputes");
        }
    }
}
