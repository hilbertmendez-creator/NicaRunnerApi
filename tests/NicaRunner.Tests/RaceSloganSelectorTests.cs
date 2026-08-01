using NicaRunner.Application.PublicResults;

namespace NicaRunner.Tests;

// design.md Decisión 6: cascada Slogan -> Descripcion -> frase determinista por hash
// FNV-1a 32-bit (nunca string.GetHashCode(), que se re-aleatoriza por proceso). Los
// vectores dorados de la sección "hash-fallback" fueron calculados independientemente
// (Python, FNV-1a 32-bit estándar) contra el arreglo Phrases de RaceSloganSelector — si
// el arreglo se reordena o cambia de tamaño, estos vectores dejan de aplicar a propósito
// (design.md: "el arreglo es append-frozen", cambiarlo es un acto deliberado y rompedor).
public class RaceSloganSelectorTests
{
    [Fact]
    public void Select_ConSloganPresente_DevuelveElSlogan()
    {
        var resultado = RaceSloganSelector.Select("Corré por tu ciudad", "Una descripción cualquiera", "AAAAAAAAAAAAAAAAAAAAAA");

        Assert.Equal("Corré por tu ciudad", resultado);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Select_ConSloganNuloOEnBlanco_CaeADescripcion(string? slogan)
    {
        var resultado = RaceSloganSelector.Select(slogan, "La carrera más esperada del año", "AAAAAAAAAAAAAAAAAAAAAA");

        Assert.Equal("La carrera más esperada del año", resultado);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("   ", "   ")]
    [InlineData(null, "   ")]
    public void Select_SinSloganNiDescripcion_CaeALaFraseDeterministaPorHash(string? slogan, string? descripcion)
    {
        var resultado = RaceSloganSelector.Select(slogan, descripcion, "AAAAAAAAAAAAAAAAAAAAAA");

        Assert.Equal("Hoy el circuito fue testigo de tu esfuerzo.", resultado);
    }

    // Vectores dorados: FNV-1a 32-bit de cada shareKey, módulo len(Phrases), calculado
    // fuera de este repo — ver comentario de clase.
    [Theory]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAA", "Hoy el circuito fue testigo de tu esfuerzo.")]
    [InlineData("BBBBBBBBBBBBBBBBBBBBBB", "Otra carrera, otra versión más fuerte de vos mismo.")]
    [InlineData("abcDEF123-_abcDEF12aa", "Correr es fácil decirlo; cruzar la meta es otra cosa.")]
    [InlineData("zzzzzzzzzzzzzzzzzzzzzz", "No corriste solo contra el reloj: corriste contra vos de ayer.")]
    public void Select_VectorDoradoPorShareKey_DevuelveLaFraseEsperada(string shareKey, string fraseEsperada)
    {
        var resultado = RaceSloganSelector.Select(null, null, shareKey);

        Assert.Equal(fraseEsperada, resultado);
    }

    [Fact]
    public void Select_EsDeterministico_MismaEntradaMismaSalidaEnDosLlamadas()
    {
        var primera = RaceSloganSelector.Select(null, null, "mismaClaveMismaClaveXX");
        var segunda = RaceSloganSelector.Select(null, null, "mismaClaveMismaClaveXX");

        Assert.Equal(primera, segunda);
    }

    [Fact]
    public void Select_SloganMasLargoQueElLimite_SeTruncaA160Caracteres()
    {
        var sloganLargo = new string('x', 300);

        var resultado = RaceSloganSelector.Select(sloganLargo, null, "AAAAAAAAAAAAAAAAAAAAAA");

        Assert.Equal(160, resultado.Length);
    }

    [Fact]
    public void Select_DescripcionMasLargaQueElLimite_SeTruncaA160Caracteres()
    {
        var descripcionLarga = new string('y', 300);

        var resultado = RaceSloganSelector.Select(null, descripcionLarga, "AAAAAAAAAAAAAAAAAAAAAA");

        Assert.Equal(160, resultado.Length);
    }
}
