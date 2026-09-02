using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NicaRunner.Infrastructure.Migrations
{
    /// <summary>
    /// Captura offline: Results gana el origen de su instante de llegada, espejo de
    /// RaceCategories.StartOrigen/StartOffsetConfianzaMs para el cero.
    ///
    /// Las dos columnas son nullable y NO se backfillean a 'Servidor'. Es deliberado: una
    /// captura anterior a esta migración se hizo online por construcción, pero nadie lo
    /// registró, y escribir un origen que nunca se midió sería inventar la evidencia que
    /// estas columnas existen para dar. Null significa "no consta", que es la verdad.
    /// </summary>
    public partial class AddResultArrivalClockOrigin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TiempoOffsetConfianzaMs",
                table: "Results",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TiempoOrigen",
                table: "Results",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TiempoOffsetConfianzaMs",
                table: "Results");

            migrationBuilder.DropColumn(
                name: "TiempoOrigen",
                table: "Results");
        }
    }
}
