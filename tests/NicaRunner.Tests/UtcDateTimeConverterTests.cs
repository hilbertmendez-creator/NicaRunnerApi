using System.Text.Json;
using NicaRunner.Api.Startup;

namespace NicaRunner.Tests;

public class UtcDateTimeConverterTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new UtcDateTimeConverter() }
    };

    private record Wrapper(DateTime Value);

    [Fact]
    public void Read_SufijoZ_QuedaComoUtcSinDesplazar()
    {
        var result = JsonSerializer.Deserialize<Wrapper>("""{"Value":"2026-08-05T10:00:00Z"}""", Options)!;

        Assert.Equal(DateTimeKind.Utc, result.Value.Kind);
        Assert.Equal(new DateTime(2026, 8, 5, 10, 0, 0, DateTimeKind.Utc), result.Value);
    }

    [Fact]
    public void Read_SinInformacionDeOffset_SeReetiquetaComoUtcSinDesplazar()
    {
        var result = JsonSerializer.Deserialize<Wrapper>("""{"Value":"2026-08-05T10:00:00"}""", Options)!;

        Assert.Equal(DateTimeKind.Utc, result.Value.Kind);
        Assert.Equal(new DateTime(2026, 8, 5, 10, 0, 0, DateTimeKind.Utc), result.Value);
    }

    // Este es el caso que rompía antes del fix: un offset explícito distinto de "Z"
    // (ej. el de Nicaragua) llega de System.Text.Json con Kind=Local y los ticks
    // reescritos a la hora local del proceso — comparar eso crudo contra
    // DateTime.UtcNow (los operadores de DateTime ignoran Kind) daba un resultado
    // silenciosamente equivocado salvo que el servidor corriera en UTC.
    [Fact]
    public void Read_ConOffsetExplicitoNoZ_ConvierteAlInstanteUtcCorrecto()
    {
        // 10:00 en -06:00 es 16:00 UTC — sin importar en qué huso corra el proceso
        // que ejecuta el test, el resultado tiene que ser ese instante exacto.
        var result = JsonSerializer.Deserialize<Wrapper>("""{"Value":"2026-08-05T10:00:00-06:00"}""", Options)!;

        Assert.Equal(DateTimeKind.Utc, result.Value.Kind);
        Assert.Equal(new DateTime(2026, 8, 5, 16, 0, 0, DateTimeKind.Utc), result.Value);
    }

    [Fact]
    public void WriteYRead_SonSimetricos_ElInstanteSobreviveElRoundtrip()
    {
        var original = new Wrapper(new DateTime(2026, 8, 5, 10, 0, 0, DateTimeKind.Utc));

        var json = JsonSerializer.Serialize(original, Options);
        var result = JsonSerializer.Deserialize<Wrapper>(json, Options)!;

        Assert.Equal(original.Value, result.Value);
        Assert.Equal(DateTimeKind.Utc, result.Value.Kind);
    }
}
