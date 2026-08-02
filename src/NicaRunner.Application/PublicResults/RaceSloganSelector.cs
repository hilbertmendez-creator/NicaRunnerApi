using System.Text;

namespace NicaRunner.Application.PublicResults;

/// <summary>
/// design.md Decisión 6: selector puro y determinista del eslogan del enlace público de
/// detalle del corredor. Cascada: <c>Race.Slogan</c> -> <c>Race.Descripcion</c> -> frase
/// fija elegida por hash estable de <c>PublicShareKey</c>. Función pura a propósito:
/// Slice 2 (previsualización Open Graph server-rendered) llama exactamente esta misma
/// función, nunca una copia — desincronizarlas desincroniza la preview cacheada de
/// WhatsApp de la página en vivo.
///
/// El hash es FNV-1a 32-bit sobre los bytes UTF-8 del shareKey, implementado acá mismo.
/// NUNCA <see cref="string.GetHashCode()"/>: desde .NET Core, ese hash se re-aleatoriza
/// en cada arranque del proceso, así que la misma clave elegiría una frase distinta cada
/// vez que reinicia el servidor — rompería justo la garantía de determinismo que este
/// selector existe para dar.
///
/// <para>
/// <c>Phrases</c> es APPEND-FROZEN: es un contrato, no una nota de estilo. Cualquier
/// runner sin Slogan/Descripcion queda mapeado a <c>hash % Phrases.Length</c>; reordenar,
/// insertar o quitar una frase reasigna la frase de TODOS los corredores existentes de
/// una sola vez. Cambiar este arreglo es un acto deliberado y rompedor (invalida
/// cualquier preview de Open Graph ya cacheada por WhatsApp) — nunca un ajuste casual.
/// </para>
/// </summary>
public static class RaceSloganSelector
{
    public const int MaxLength = 160;

    // APPEND-FROZEN — ver comentario de clase. Solo se agrega al final; nunca se
    // reordena, inserta en medio, ni se borra un elemento existente.
    private static readonly string[] Phrases =
    [
        "Cada paso cuenta, y hoy diste todos los que hacían falta.",
        "El asfalto ya sabe tu nombre.",
        "Correr es fácil decirlo; cruzar la meta es otra cosa.",
        "Otra carrera, otra versión más fuerte de vos mismo.",
        "El cansancio se olvida; la meta cruzada, no.",
        "No corriste solo contra el reloj: corriste contra vos de ayer.",
        "Cada kilómetro fue una decisión de no rendirte.",
        "La meta no premia al más rápido, premia al que no paró.",
        "Sudaste la camiseta y se nota en el resultado.",
        "Hoy el circuito fue testigo de tu esfuerzo.",
    ];

    public static string Select(string? slogan, string? descripcion, string shareKey)
    {
        if (!string.IsNullOrWhiteSpace(slogan))
            return Truncate(slogan);

        if (!string.IsNullOrWhiteSpace(descripcion))
            return Truncate(descripcion);

        var index = (int)(Fnv1a32(shareKey) % (uint)Phrases.Length);
        return Phrases[index];
    }

    private static string Truncate(string value) =>
        value.Length <= MaxLength ? value : value[..MaxLength];

    // FNV-1a de 32 bits (offset basis 2166136261, prime 16777619) sobre UTF-8 — mismo
    // algoritmo estándar, implementado inline para no traer una dependencia por 6 líneas.
    private static uint Fnv1a32(string value)
    {
        const uint offsetBasis = 2166136261;
        const uint prime = 16777619;

        var hash = offsetBasis;
        foreach (var b in Encoding.UTF8.GetBytes(value))
        {
            hash ^= b;
            hash *= prime;
        }

        return hash;
    }
}
