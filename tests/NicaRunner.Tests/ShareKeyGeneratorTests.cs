using System.Text.RegularExpressions;
using NicaRunner.Application.Common;

namespace NicaRunner.Tests;

// design.md Decisión 1: RandomNumberGenerator.GetBytes(16) -> Base64Url, 22 caracteres,
// sin padding ni caracteres inseguros para URL. Nunca Guid.NewGuid() (documenta unicidad,
// no impredecibilidad).
public class ShareKeyGeneratorTests
{
    private static readonly Regex Base64UrlAlphabet = new("^[A-Za-z0-9_-]+$");

    [Fact]
    public void Generate_DevuelveVeintidosCaracteres()
    {
        var key = ShareKeyGenerator.Generate();

        Assert.Equal(22, key.Length);
    }

    [Fact]
    public void Generate_SoloUsaElAlfabetoBase64UrlSeguroParaUrl()
    {
        var key = ShareKeyGenerator.Generate();

        Assert.Matches(Base64UrlAlphabet, key);
    }

    [Fact]
    public void Generate_NuncaContienePaddingNiCaracteresBase64Estandar()
    {
        var key = ShareKeyGenerator.Generate();

        Assert.DoesNotContain('=', key);
        Assert.DoesNotContain('+', key);
        Assert.DoesNotContain('/', key);
    }

    [Fact]
    public void Generate_EnCienMilMuestras_NoProduceColisiones()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        for (var i = 0; i < 100_000; i++)
            Assert.True(seen.Add(ShareKeyGenerator.Generate()), "Colisión inesperada en la muestra.");
    }
}
