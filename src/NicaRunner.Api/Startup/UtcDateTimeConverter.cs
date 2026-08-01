using System.Text.Json;
using System.Text.Json.Serialization;

namespace NicaRunner.Api.Startup;

/// <summary>
/// Sqlite y Npgsql devuelven DateTime con Kind=Unspecified (no preservan el
/// Kind al leer), y System.Text.Json serializa eso sin sufijo "Z" — el
/// navegador interpreta la hora como LOCAL en vez de UTC, causando un desfase
/// de 6h en Nicaragua. Todas las columnas DateTime de la app almacenan
/// instantes UTC, así que Unspecified se re-etiqueta como Utc sin conversión;
/// Local (poco común) sí se convierte. Se aplica también a DateTime? porque
/// System.Text.Json envuelve automáticamente los converters de tipos de valor
/// para su forma Nullable<T>.
/// </summary>
public sealed class UtcDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.GetDateTime();

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        var utc = value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
            : value.ToUniversalTime();
        writer.WriteStringValue(utc);
    }
}
