using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NicaRunner.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryCatalogAndRunnerDemographics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Catálogo global nuevo. Se crea primero para poder respaldar dentro de
            // él los datos de las RaceCategories existentes (una fila por categoría
            // que hoy pertenece a una sola carrera) antes de tocar esa tabla.
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Codigo = table.Column<string>(type: "TEXT", nullable: false),
                    NombreCategoria = table.Column<string>(type: "TEXT", nullable: false),
                    Descripcion = table.Column<string>(type: "TEXT", nullable: true),
                    Distancia = table.Column<decimal>(type: "TEXT", nullable: false),
                    EdadMinima = table.Column<int>(type: "INTEGER", nullable: false),
                    EdadMaxima = table.Column<int>(type: "INTEGER", nullable: false),
                    Orden = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            // Respaldo de datos: cada RaceCategory existente se convierte en una entrada
            // del catálogo global, preservando su Id (así RaceCategories.CategoryId puede
            // apuntar de vuelta con un simple "= Id", sin tabla de mapeo aparte). El código
            // corto se genera a partir del Id porque no existía antes; el admin puede
            // renombrarlo luego desde la pantalla de categorías.
            migrationBuilder.Sql(
                "INSERT INTO \"Categories\" (\"Id\", \"Codigo\", \"NombreCategoria\", \"Descripcion\", \"Distancia\", \"EdadMinima\", \"EdadMaxima\", \"Orden\") " +
                "SELECT \"Id\", 'CAT' || \"Id\", \"NombreCategoria\", NULL, \"Distancia\", \"EdadMinima\", \"EdadMaxima\", \"Orden\" FROM \"RaceCategories\";");

            migrationBuilder.DropForeignKey(
                name: "FK_Results_RaceCategories_CategoryId",
                table: "Results");

            migrationBuilder.DropForeignKey(
                name: "FK_Runners_RaceCategories_CategoryId",
                table: "Runners");

            migrationBuilder.DropIndex(
                name: "IX_RaceCategories_RaceId",
                table: "RaceCategories");

            migrationBuilder.DropColumn(
                name: "Distancia",
                table: "RaceCategories");

            migrationBuilder.DropColumn(
                name: "EdadMaxima",
                table: "RaceCategories");

            migrationBuilder.DropColumn(
                name: "EdadMinima",
                table: "RaceCategories");

            migrationBuilder.DropColumn(
                name: "NombreCategoria",
                table: "RaceCategories");

            migrationBuilder.RenameColumn(
                name: "Orden",
                table: "RaceCategories",
                newName: "CategoryId");

            // RaceCategories.CategoryId reemplaza al viejo Orden (mismo tipo int); como
            // la Category de respaldo se creó con Category.Id = RaceCategory.Id, apuntar
            // de vuelta es tan simple como igualarlo a su propio Id.
            migrationBuilder.Sql("UPDATE \"RaceCategories\" SET \"CategoryId\" = \"Id\";");

            migrationBuilder.AddColumn<string>(
                name: "Apellidos",
                table: "Runners",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Club",
                table: "Runners",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaNacimiento",
                table: "Runners",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Sexo",
                table: "Runners",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RaceCategories_CategoryId",
                table: "RaceCategories",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_RaceCategories_RaceId_CategoryId",
                table: "RaceCategories",
                columns: new[] { "RaceId", "CategoryId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Codigo",
                table: "Categories",
                column: "Codigo",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_RaceCategories_Categories_CategoryId",
                table: "RaceCategories",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Results_Categories_CategoryId",
                table: "Results",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Runners_Categories_CategoryId",
                table: "Runners",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Mismo origen que FixPostgresIdentityColumns/FixPostgresDecimalColumns/
            // FixPostgresDateTimeColumns: esta migración se generó con Sqlite activo,
            // así que Categories.Id no queda como IDENTITY en Postgres (INSERTs sin Id
            // explícito fallarían), y Categories.Distancia / Runners.FechaNacimiento
            // quedan como columnas "text" que Npgsql no puede leer como decimal/DateTime.
            if (migrationBuilder.ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                migrationBuilder.Sql("ALTER TABLE \"Categories\" ALTER COLUMN \"Id\" ADD GENERATED BY DEFAULT AS IDENTITY;");
                migrationBuilder.Sql(
                    "SELECT setval(pg_get_serial_sequence('\"Categories\"', 'Id'), " +
                    "COALESCE((SELECT MAX(\"Id\") FROM \"Categories\"), 0) + 1, false);");
                migrationBuilder.Sql("ALTER TABLE \"Categories\" ALTER COLUMN \"Distancia\" TYPE numeric USING \"Distancia\"::numeric;");
                migrationBuilder.Sql(
                    "ALTER TABLE \"Runners\" ALTER COLUMN \"FechaNacimiento\" TYPE timestamp without time zone " +
                    "USING \"FechaNacimiento\"::timestamp without time zone;");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RaceCategories_Categories_CategoryId",
                table: "RaceCategories");

            migrationBuilder.DropForeignKey(
                name: "FK_Results_Categories_CategoryId",
                table: "Results");

            migrationBuilder.DropForeignKey(
                name: "FK_Runners_Categories_CategoryId",
                table: "Runners");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_RaceCategories_CategoryId",
                table: "RaceCategories");

            migrationBuilder.DropIndex(
                name: "IX_RaceCategories_RaceId_CategoryId",
                table: "RaceCategories");

            migrationBuilder.DropColumn(
                name: "Apellidos",
                table: "Runners");

            migrationBuilder.DropColumn(
                name: "Club",
                table: "Runners");

            migrationBuilder.DropColumn(
                name: "FechaNacimiento",
                table: "Runners");

            migrationBuilder.DropColumn(
                name: "Sexo",
                table: "Runners");

            migrationBuilder.RenameColumn(
                name: "CategoryId",
                table: "RaceCategories",
                newName: "Orden");

            migrationBuilder.AddColumn<decimal>(
                name: "Distancia",
                table: "RaceCategories",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "EdadMaxima",
                table: "RaceCategories",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EdadMinima",
                table: "RaceCategories",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "NombreCategoria",
                table: "RaceCategories",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_RaceCategories_RaceId",
                table: "RaceCategories",
                column: "RaceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Results_RaceCategories_CategoryId",
                table: "Results",
                column: "CategoryId",
                principalTable: "RaceCategories",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Runners_RaceCategories_CategoryId",
                table: "Runners",
                column: "CategoryId",
                principalTable: "RaceCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
