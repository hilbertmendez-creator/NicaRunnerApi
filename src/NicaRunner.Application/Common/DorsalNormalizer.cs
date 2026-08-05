using System.Text.RegularExpressions;

namespace NicaRunner.Application.Common;

// design.md D11: función pura, aplicada en TODA escritura de Runner.Dorsal y
// ReservedDorsal.Dorsal (confirm individual, confirm bulk, y el manual
// RunnerService.CreateAsync/UpdateAsync). Opera sobre el string FINAL ya tipeado por el
// admin, no sobre un prefijo/número separados — no existe generación de dorsal (D7), así
// que no hay componentes que extraer por separado.
//
// "21K007" -> "21K7";  "0101" -> "101";  "5K1234" -> "5K1234";  "VIP-1" -> "VIP-1"
//
// Un dorsal sin cola numérica, o que contenga cualquier carácter fuera de [A-Z0-9]
// (p.ej. "VIP-1"), pasa recortado/mayúsculas sin ganar una regla de formato — sigue
// cubierto por la unicidad, solo que sin la equivalencia numérica.
public static class DorsalNormalizer
{
    private static readonly Regex TrailingDigits = new(@"^([A-Z0-9]*?)(\d+)$", RegexOptions.Compiled);

    public static string Normalize(string dorsal)
    {
        var raw = dorsal.Trim().ToUpperInvariant();
        var match = TrailingDigits.Match(raw);
        return match.Success
            ? match.Groups[1].Value + long.Parse(match.Groups[2].Value)
            : raw;
    }
}
