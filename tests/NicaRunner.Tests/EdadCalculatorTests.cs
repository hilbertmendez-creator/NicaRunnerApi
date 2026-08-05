using NicaRunner.Application.Common;

namespace NicaRunner.Tests;

// design.md D6: extraído de RunnerService.CalculateEdad (privado). Misma lógica exacta
// (años completos entre fechaNacimiento y asOf), pero reutilizable desde
// RegistrationService sin duplicar el cálculo. asOf es SIEMPRE Race.FechaCarrera, nunca
// DateTime.Now — la clasificación de categoría/minoría de edad no debe moverse con el
// paso del tiempo entre sumisión y revisión.
public class EdadCalculatorTests
{
    [Fact]
    public void AtRaceDate_CumpleanosYaPasadoEnElAnio_CalculaEdadCompleta()
    {
        var fechaNacimiento = new DateTime(2000, 1, 1);
        var fechaCarrera = new DateTime(2026, 6, 1);

        Assert.Equal(26, EdadCalculator.AtRaceDate(fechaNacimiento, fechaCarrera));
    }

    [Fact]
    public void AtRaceDate_CumpleanosAunNoLlegaEnElAnioDeLaCarrera_RestaUnAnio()
    {
        var fechaNacimiento = new DateTime(2000, 12, 31);
        var fechaCarrera = new DateTime(2026, 6, 1);

        Assert.Equal(25, EdadCalculator.AtRaceDate(fechaNacimiento, fechaCarrera));
    }

    [Fact]
    public void AtRaceDate_MismaFechaExacta_CalculaEdadCompleta()
    {
        var fechaNacimiento = new DateTime(2000, 6, 1);
        var fechaCarrera = new DateTime(2026, 6, 1);

        Assert.Equal(26, EdadCalculator.AtRaceDate(fechaNacimiento, fechaCarrera));
    }

    [Fact]
    public void AtRaceDate_NuncaDevuelveNegativo()
    {
        var fechaNacimiento = new DateTime(2030, 1, 1);
        var fechaCarrera = new DateTime(2026, 6, 1);

        Assert.Equal(0, EdadCalculator.AtRaceDate(fechaNacimiento, fechaCarrera));
    }

    [Fact]
    public void AtRaceDate_DiaAnteriorAlCumpleanos_RestaUnAnio()
    {
        var fechaNacimiento = new DateTime(2010, 6, 2);
        var fechaCarrera = new DateTime(2026, 6, 1);

        Assert.Equal(15, EdadCalculator.AtRaceDate(fechaNacimiento, fechaCarrera));
    }
}
