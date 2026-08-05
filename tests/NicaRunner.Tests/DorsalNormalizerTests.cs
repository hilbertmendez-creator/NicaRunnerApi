using NicaRunner.Application.Common;

namespace NicaRunner.Tests;

// design.md D11: DorsalNormalizer.Normalize es una función pura, aplicada en toda
// escritura de Runner.Dorsal y ReservedDorsal.Dorsal. La equivalencia numérica ("21K7"
// == "21K007") es lo que el índice único aditivo IX_Runners_RaceId_DorsalNormalizado
// termina de aplicar contra la base real (ver DorsalNormalizationIntegrationTests).
public class DorsalNormalizerTests
{
    [Theory]
    [InlineData("21K007", "21K7")]
    [InlineData("21K7", "21K7")]
    [InlineData("0101", "101")]
    [InlineData("101", "101")]
    [InlineData("5K1234", "5K1234")]
    [InlineData("VIP-1", "VIP-1")]
    public void Normalize_CasosDelDesign_CoincidenExactamente(string dorsal, string esperado)
    {
        Assert.Equal(esperado, DorsalNormalizer.Normalize(dorsal));
    }

    [Fact]
    public void Normalize_DosVariantesConPaddingDistinto_NormalizanIgual()
    {
        Assert.Equal(DorsalNormalizer.Normalize("21K007"), DorsalNormalizer.Normalize("21K7"));
        Assert.Equal(DorsalNormalizer.Normalize("0101"), DorsalNormalizer.Normalize("101"));
    }

    [Theory]
    [InlineData("  21k007  ", "21K7")]
    [InlineData("vip-1", "VIP-1")]
    public void Normalize_RecortaEspaciosYNormalizaMayusculas(string dorsal, string esperado)
    {
        Assert.Equal(esperado, DorsalNormalizer.Normalize(dorsal));
    }

    [Fact]
    public void Normalize_DorsalSinColaNumerica_PasaSinCambios()
    {
        Assert.Equal("VIP", DorsalNormalizer.Normalize("vip"));
    }

    [Fact]
    public void Normalize_PrefijoDeUnSoloCaracterConCerosALaIzquierda_StripsCorrectamente()
    {
        Assert.Equal("A1", DorsalNormalizer.Normalize("A001"));
    }
}
