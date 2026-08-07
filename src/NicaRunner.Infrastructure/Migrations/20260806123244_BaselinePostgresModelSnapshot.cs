using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NicaRunner.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BaselinePostgresModelSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Esta migración hace dos cosas distintas.
            //
            // 1) Un cambio real de esquema: agrega las columnas de la máquina de estados
            // de RaceCategory (Estado, StartUtc, StartedByUserId, ClosedUtc,
            // ClosedByUserId) con sus índices y FKs hacia Users — necesarias para el
            // timing por categoría. Esto SÍ ejecuta SQL contra la base real.
            //
            // 2) Todo lo demás queda intencionalmente vacío. El snapshot de EF quedó
            // desincronizado del estado real de Postgres desde que el proyecto generaba
            // migraciones contra Sqlite por error (bug de origen, corregido en un PR
            // previo). Esa deriva es cosmética (ej. TEXT vs character varying(30) en
            // columnas de texto simple) y nunca causó un bug real — a diferencia de las
            // columnas que sí rompían algo (identidad, booleano, fecha, decimal), que ya
            // se corrigieron con SQL crudo en migraciones previas (FixPostgres*) y tienen
            // configuración explícita en OnModelCreating. No ejecutamos ningún SQL para
            // esa parte: solo le decimos a EF "el modelo actual y la base ya están de
            // acuerdo", para que el snapshot deje de arrastrar tipos de Sqlite y las
            // migraciones futuras vuelvan a ser diffs chicos y revisables.
            //
            // TimingDisputes.UpdatedAt/ResolvedAt y AuditLogs.CreatedAt quedan
            // deliberadamente sin tocar: son columnas de texto guardando fechas, un bug
            // real y separado que necesita su propia migración de datos, no una
            // reconciliación de metadata como esta.
            migrationBuilder.AddColumn<int>(
                name: "ClosedByUserId",
                table: "RaceCategories",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ClosedUtc",
                table: "RaceCategories",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Estado",
                table: "RaceCategories",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Planeada");

            migrationBuilder.AddColumn<DateTime>(
                name: "StartUtc",
                table: "RaceCategories",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StartedByUserId",
                table: "RaceCategories",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RaceCategories_ClosedByUserId",
                table: "RaceCategories",
                column: "ClosedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RaceCategories_StartedByUserId",
                table: "RaceCategories",
                column: "StartedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_RaceCategories_Users_ClosedByUserId",
                table: "RaceCategories",
                column: "ClosedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RaceCategories_Users_StartedByUserId",
                table: "RaceCategories",
                column: "StartedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Simétrico a Up(): revierte únicamente el cambio real (columnas/índices/FKs
            // de RaceCategory). El resto no ejecuta nada porque Up() tampoco lo hizo.
            migrationBuilder.DropForeignKey(
                name: "FK_RaceCategories_Users_ClosedByUserId",
                table: "RaceCategories");

            migrationBuilder.DropForeignKey(
                name: "FK_RaceCategories_Users_StartedByUserId",
                table: "RaceCategories");

            migrationBuilder.DropIndex(
                name: "IX_RaceCategories_ClosedByUserId",
                table: "RaceCategories");

            migrationBuilder.DropIndex(
                name: "IX_RaceCategories_StartedByUserId",
                table: "RaceCategories");

            migrationBuilder.DropColumn(
                name: "ClosedByUserId",
                table: "RaceCategories");

            migrationBuilder.DropColumn(
                name: "ClosedUtc",
                table: "RaceCategories");

            migrationBuilder.DropColumn(
                name: "Estado",
                table: "RaceCategories");

            migrationBuilder.DropColumn(
                name: "StartUtc",
                table: "RaceCategories");

            migrationBuilder.DropColumn(
                name: "StartedByUserId",
                table: "RaceCategories");
        }
    }
}
