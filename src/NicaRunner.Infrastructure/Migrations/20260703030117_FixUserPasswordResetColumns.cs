using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NicaRunner.Infrastructure.Migrations
{
    /// <summary>
    /// Mismo origen que FixPostgresBooleanColumns/FixPostgresDateTimeColumns:
    /// AddUserPasswordResetFields (20260701121914) se generó con Sqlite activo,
    /// que graba bool como INTEGER y DateTime como TEXT. Esos nombres de tipo
    /// también son válidos en Postgres, así que el ALTER TABLE original no
    /// falló, pero cualquier SELECT sobre "Users" ahora lanza InvalidCastException
    /// al leer MustChangePassword/PasswordResetTokenExpiry — rompiendo el seed de
    /// administradores y el login en producción. PasswordResetToken (string/TEXT)
    /// no necesita corrección: "text" es el tipo nativo correcto en Postgres.
    /// </summary>
    public partial class FixUserPasswordResetColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider != "Npgsql.EntityFrameworkCore.PostgreSQL")
                return;

            migrationBuilder.Sql(
                "ALTER TABLE \"Users\" ALTER COLUMN \"MustChangePassword\" DROP DEFAULT;");

            migrationBuilder.Sql(
                "ALTER TABLE \"Users\" ALTER COLUMN \"MustChangePassword\" TYPE boolean " +
                "USING CASE WHEN \"MustChangePassword\" = 0 THEN false ELSE true END;");

            migrationBuilder.Sql(
                "ALTER TABLE \"Users\" ALTER COLUMN \"MustChangePassword\" SET DEFAULT false;");

            migrationBuilder.Sql(
                "ALTER TABLE \"Users\" ALTER COLUMN \"PasswordResetTokenExpiry\" TYPE timestamp without time zone " +
                "USING \"PasswordResetTokenExpiry\"::timestamp without time zone;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider != "Npgsql.EntityFrameworkCore.PostgreSQL")
                return;

            migrationBuilder.Sql(
                "ALTER TABLE \"Users\" ALTER COLUMN \"MustChangePassword\" DROP DEFAULT;");

            migrationBuilder.Sql(
                "ALTER TABLE \"Users\" ALTER COLUMN \"MustChangePassword\" TYPE integer " +
                "USING CASE WHEN \"MustChangePassword\" THEN 1 ELSE 0 END;");

            migrationBuilder.Sql(
                "ALTER TABLE \"Users\" ALTER COLUMN \"MustChangePassword\" SET DEFAULT 0;");

            migrationBuilder.Sql(
                "ALTER TABLE \"Users\" ALTER COLUMN \"PasswordResetTokenExpiry\" TYPE text " +
                "USING \"PasswordResetTokenExpiry\"::text;");
        }
    }
}
