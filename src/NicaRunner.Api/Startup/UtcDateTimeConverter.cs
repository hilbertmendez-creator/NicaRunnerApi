using System.Text.Json;
using System.Text.Json.Serialization;

namespace NicaRunner.Api.Startup;

/// <summary>
/// Sqlite y Npgsql devuelven DateTime sin preservar Kind, y System.Text.Json
/// serializa eso sin sufijo "Z" — el navegador interpreta la hora como LOCAL
/// en vez de UTC, causando un desfase de 6h en Nicaragua. Todas las columnas
/// DateTime de la app almacenan instantes UTC, así que acá se re-etiquetan
/// como Utc sin conversión de reloj cuando ya vienen Unspecified.
///
/// Las 17 columnas "timestamp without time zone" (ver UtcDateTimeValueConverter
/// en NicaRunnerDbContext) ya llegan acá con Kind=Utc — ese converter de EF
/// hace su propio relabel en la lectura, antes de que el valor llegue a esta
/// capa. El chequeo de Kind de abajo sigue siendo necesario para cualquier
/// DateTime que NO pase por ese converter (ej. columnas todavía TEXT/Sqlite-era
/// sin reconciliar), donde Kind puede seguir llegando Unspecified.
///
/// Se aplica también a DateTime? porque System.Text.Json envuelve
/// automáticamente los converters de tipos de valor para su forma Nullable<T>.
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
