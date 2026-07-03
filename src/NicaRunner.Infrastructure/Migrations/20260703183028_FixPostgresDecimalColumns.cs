using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NicaRunner.Infrastructure.Migrations
{
    /// <summary>
    /// Cuarta corrección del mismo origen que FixPostgresIdentity/Boolean/DateTime
    /// Columns: las migraciones se generaron con Sqlite activo, y Sqlite serializa
    /// decimal como TEXT (ISO). La anotación "TEXT" quedó literal en la
    /// InitialCreate ("Distancia = table.Column&lt;decimal&gt;(type: \"TEXT\", ...)"),
    /// y Postgres la crea como columna text sin protestar. Pero Npgsql no puede
    /// leer una columna text como decimal — cualquier SELECT que materialice un
    /// RaceCategory revienta con InvalidCastException, y por transitividad rompe
    /// también los endpoints que hacen Include(r =&gt; r.Category) sobre Runners
    /// y Results (encontrado con smoke E2E de idempotency: POST /runners con
    /// dorsal cargaba la categoría y explotaba en 500).
    ///
    /// Única columna decimal en el modelo hoy es RaceCategories.Distancia. Si
    /// se agregan más adelante, se listan acá.
    /// </summary>
    public partial class FixPostgresDecimalColumns : Migration
    {
        private static readonly (string Table, string Column)[] DecimalColumns =
        {
            ("RaceCategories", "Distancia")
        };

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider != "Npgsql.EntityFrameworkCore.PostgreSQL")
                return;

            foreach (var (table, column) in DecimalColumns)
            {
                migrationBuilder.Sql(
                    $"ALTER TABLE \"{table}\" ALTER COLUMN \"{column}\" TYPE numeric USING \"{column}\"::numeric;");
            }
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider != "Npgsql.EntityFrameworkCore.PostgreSQL")
                return;

            foreach (var (table, column) in DecimalColumns)
            {
                migrationBuilder.Sql($"ALTER TABLE \"{table}\" ALTER COLUMN \"{column}\" TYPE text USING \"{column}\"::text;");
            }
        }
    }
}
