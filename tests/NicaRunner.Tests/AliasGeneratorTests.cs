using NicaRunner.Application.Common;

namespace NicaRunner.Tests;

// Fixtures congeladas desde design.md §1.7 ("Worked examples") — cada fila es el
// contrato de comportamiento del generador, no un detalle de implementación.
public class AliasGeneratorTests
{
    [Theory]
    [InlineData("Hilbert Mendez Velasquez", "no-usado@x.com", "hmendezv")]
    [InlineData("María José Pérez López", "no-usado@x.com", "mperezl")]
    [InlineData("Hilbert Mendez", "no-usado@x.com", "hmendez")]
    [InlineData("Juan de la Cruz Pérez", "no-usado@x.com", "jdelacruzp")]
    [InlineData("Ana van Dijk Ruiz", "no-usado@x.com", "avandijkr")]
    [InlineData("Ana Pérez-León Ruiz", "no-usado@x.com", "aperezleonr")]
    [InlineData("Prince", "no-usado@x.com", "prince")]
    [InlineData("Ed", "no-usado@x.com", "edx")]
    [InlineData("Li Bo", "no-usado@x.com", "lbo")]
    [InlineData("A", "no-usado@x.com", "axx")]
    [InlineData("李雷", "jl.li@x.com", "jlli")]
    [InlineData("hilbert.mendez@gmail.com", "hilbert.mendez@gmail.com", "hilbertmendez")]
    public void Generate_EjemplosDeDesignMd_ProduceElAliasEsperado(string nombre, string email, string esperado) =>
        Assert.Equal(esperado, AliasGenerator.Generate(nombre, email));

    [Fact]
    public void Generate_EsDeterministico_MismaEntradaMismaSalida()
    {
        var a = AliasGenerator.Generate("Hilbert Mendez Velasquez", "hilbert@x.com");
        var b = AliasGenerator.Generate("Hilbert Mendez Velasquez", "hilbert@x.com");

        Assert.Equal(a, b);
    }

    [Theory]
    [InlineData(2, "hmendezv2")]
    [InlineData(3, "hmendezv3")]
    [InlineData(10, "hmendezv10")]
    public void Generate_ConIntentoMayorA1_AgregaSufijoNumerico(int attempt, string esperado) =>
        Assert.Equal(esperado, AliasGenerator.Generate("Hilbert Mendez Velasquez", "no-usado@x.com", attempt));

    [Fact]
    public void Generate_NuncaSuperaElMaximoDe30_InclusoConSufijoDeDosDigitos()
    {
        // Apellido larguísimo: el recorte debe dejar espacio para inicial + sufijo.
        var nombre = "Alejandro Interminabledeveintiochocaracteresmas Ordoñez";

        var candidate = AliasGenerator.Generate(nombre, "no-usado@x.com", attempt: 10);

        Assert.True(candidate.Length <= 30, $"'{candidate}' mide {candidate.Length} caracteres.");
        Assert.EndsWith("10", candidate);
        Assert.StartsWith("a", candidate);
    }

    [Fact]
    public void Generate_CeroTokensYFallbackDeEmailTambienVacio_UsaLiteralUser()
    {
        // Nombre y email local-part normalizan a cero unidades: fallback final "user".
        var candidate = AliasGenerator.Generate("李雷", "李雷@x.com");

        Assert.Equal("user", candidate);
    }

    [Theory]
    [InlineData("hmendezv")]
    [InlineData("h.mendez-v_2")]
    [InlineData("abc")]
    public void IsValidAliasFormat_ValoresValidos_DevuelveTrue(string alias) =>
        Assert.True(AliasGenerator.IsValidAliasFormat(alias));

    [Theory]
    [InlineData("ab")] // < 3 caracteres
    [InlineData("usuario@dominio")] // contiene '@'
    [InlineData("HMendez")] // mayúsculas
    [InlineData("")]
    public void IsValidAliasFormat_ValoresInvalidos_DevuelveFalse(string alias) =>
        Assert.False(AliasGenerator.IsValidAliasFormat(alias));

    [Fact]
    public void IsValidAliasFormat_TreintaYUnCaracteres_DevuelveFalse() =>
        Assert.False(AliasGenerator.IsValidAliasFormat(new string('a', 31)));
}
