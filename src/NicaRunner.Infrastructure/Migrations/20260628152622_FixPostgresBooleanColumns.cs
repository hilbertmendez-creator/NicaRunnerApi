using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NicaRunner.Infrastructure.Migrations
{
    /// <summary>
    /// Mismo origen que FixPostgresIdentityColumns: las migraciones anteriores
    /// se generaron con Sqlite activo, donde un bool se almacena como
    /// INTEGER (0/1) — esa anotación de tipo quedó grabada literalmente en
    /// las migrations y "INTEGER" también es un nombre de tipo válido en
    /// Postgres, así que no fallaba al aplicar el ALTER TABLE, pero el
    /// modelo EF sigue mapeando la propiedad C# como bool/Npgsql boolean,
    /// y Postgres rechaza la escritura: "column is of type integer but
    /// expression is of type boolean". Detectado probando un INSERT real
    /// (registro de usuario) contra un Postgres real antes de desplegar.
    /// </summary>
    public partial class FixPostgresBooleanColumns : Migration
    {
        private static readonly (string Table, string Column)[] BoolColumns =
        {
            ("Users", "IsActive"),
            ("PublicResultTokens", "IsExpired")
        };

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider != "Npgsql.EntityFrameworkCore.PostgreSQL")
                return;

            foreach (var (table, column) in BoolColumns)
            {
                migrationBuilder.Sql(
                    $"ALTER TABLE \"{table}\" ALTER COLUMN \"{column}\" TYPE boolean " +
                    $"USING CASE WHEN \"{column}\" = 0 THEN false ELSE true END;");
            }
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider != "Npgsql.EntityFrameworkCore.PostgreSQL")
                return;

            foreach (var (table, column) in BoolColumns)
            {
                migrationBuilder.Sql(
                    $"ALTER TABLE \"{table}\" ALTER COLUMN \"{column}\" TYPE integer " +
                    $"USING CASE WHEN \"{column}\" THEN 1 ELSE 0 END;");
            }
        }
    }
}
