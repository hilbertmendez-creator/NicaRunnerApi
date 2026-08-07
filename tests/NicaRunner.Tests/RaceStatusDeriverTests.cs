using NicaRunner.Domain.Entities;

namespace NicaRunner.Tests;

public class RaceStatusDeriverTests
{
    private static RaceCategory Cat(RaceCategoryStatus estado) => new() { Estado = estado };

    [Fact]
    public void SinCategorias_EsPlaneada()
    {
        Assert.Equal(RaceStatus.Planeada, RaceStatusDeriver.From([]));
    }

    [Fact]
    public void TodasPlaneadas_EsPlaneada()
    {
        var categories = new[] { Cat(RaceCategoryStatus.Planeada), Cat(RaceCategoryStatus.Planeada) };

        Assert.Equal(RaceStatus.Planeada, RaceStatusDeriver.From(categories));
    }

    [Fact]
    public void AlMenosUnaEnCurso_EsEnCurso()
    {
        var categories = new[] { Cat(RaceCategoryStatus.Planeada), Cat(RaceCategoryStatus.EnCurso) };

        Assert.Equal(RaceStatus.EnCurso, RaceStatusDeriver.From(categories));
    }

    [Fact]
    public void TodasTerminadas_EsTerminada()
    {
        var categories = new[] { Cat(RaceCategoryStatus.Terminada), Cat(RaceCategoryStatus.Terminada) };

        Assert.Equal(RaceStatus.Terminada, RaceStatusDeriver.From(categories));
    }

    // El caso que la tabla del spec no cubría: una ola ya terminó, otra todavía no
    // salió, ninguna está corriendo ahora mismo. La carrera está en marcha.
    [Fact]
    public void AlgunaTerminadaYRestoPlaneada_EsEnCurso()
    {
        var categories = new[] { Cat(RaceCategoryStatus.Terminada), Cat(RaceCategoryStatus.Planeada) };

        Assert.Equal(RaceStatus.EnCurso, RaceStatusDeriver.From(categories));
    }

    [Fact]
    public void MezclaConEnCursoYTerminada_EsEnCurso()
    {
        var categories = new[] { Cat(RaceCategoryStatus.Terminada), Cat(RaceCategoryStatus.EnCurso) };

        Assert.Equal(RaceStatus.EnCurso, RaceStatusDeriver.From(categories));
    }
}
