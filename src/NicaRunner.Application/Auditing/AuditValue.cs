using System.Globalization;

namespace NicaRunner.Application.Auditing;

/// <summary>
/// Serializa valores a una representación textual estable y comparable (cultura
/// invariante), para que "anterior" y "nuevo" no generen falsos diffs por formato
/// regional (p. ej. coma vs. punto decimal) y sean legibles en la bitácora.
/// </summary>
public static class AuditValue
{
    public static string? Of(string? value) => value;
    public static string Of(decimal value) => value.ToString(CultureInfo.InvariantCulture);
    public static string Of(int value) => value.ToString(CultureInfo.InvariantCulture);
    public static string Of(bool value) => value ? "true" : "false";
    public static string Of(DateTime value) => value.ToUniversalTime().ToString("O");
    public static string Of<TEnum>(TEnum value) where TEnum : struct, Enum => value.ToString();
}
