using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NicaRunner.Infrastructure.Migrations
{
    /// <summary>
    /// Mismo origen que FixPostgresDateTimeColumns/FixUserPasswordResetColumns: esta
    /// migración (y todas las anteriores en este proyecto) se autoría sin un Postgres
    /// vivo contra el que correr `dotnet ef migrations add`, así que AddUserUsername
    /// grabó LockedUntilUtc/LastFailedLoginUtc con la anotación de tipo "TEXT" que usa
    /// Sqlite para DateTime. "TEXT" también es un nombre de tipo válido en Postgres (el
    /// ALTER TABLE no falla), pero el modelo EF sigue mapeando ambas propiedades como
    /// DateTime?/Npgsql timestamp, y cualquier SELECT sobre "Users" lanzaría
    /// InvalidCastException -- exactamente el bug ya corregido 3 veces en este historial
    /// de migraciones (Identity/Boolean/DateTime), aplicado preventivamente acá para no
    /// repetirlo una cuarta vez. Username (string/TEXT) no necesita corrección: "text" es
    /// el tipo nativo correcto en Postgres. FailedLoginCount (int/INTEGER) tampoco: es un
    /// nombre de tipo Postgres válido para un entero corriente.
    /// </summary>
    public partial class FixPostgresLockoutColumns : Migration
    {
        private static readonly string[] DateTimeColumns = ["LockedUntilUtc", "LastFailedLoginUtc"];

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider != "Npgsql.EntityFrameworkCore.PostgreSQL")
                return;

            foreach (var column in DateTimeColumns)
            {
                migrationBuilder.Sql(
                    $"ALTER TABLE \"Users\" ALTER COLUMN \"{column}\" TYPE timestamp without time zone " +
                    $"USING \"{column}\"::timestamp without time zone;");
            }
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider != "Npgsql.EntityFrameworkCore.PostgreSQL")
                return;

            foreach (var column in DateTimeColumns)
            {
                migrationBuilder.Sql($"ALTER TABLE \"Users\" ALTER COLUMN \"{column}\" TYPE text USING \"{column}\"::text;");
            }
        }
    }
}
