using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace NicaRunner.Application.Common;

/// <summary>
/// Generador puro y determinista de alias (Username) a partir del nombre completo
/// de un usuario. No depende de la base de datos ni de I/O y no lanza excepciones
/// para ninguna entrada — la unicidad la resuelve <see cref="AliasAssigner"/>
/// probando candidatos contra el repositorio. Ver design.md Parte 1 (algoritmo de
/// alias) para el detalle y la justificación de cada regla.
/// </summary>
public static class AliasGenerator
{
    public const int MaxLength = 30;
    public const int MinLength = 3;

    // Conjunto cerrado de partículas nobiliarias — se pliegan siempre hacia la
    // siguiente unidad, nunca son una unidad por sí solas (design.md §1.2).
    private static readonly HashSet<string> NobiliaryParticles = new(StringComparer.Ordinal)
    {
        "de", "del", "la", "las", "los", "y", "e", "da", "das", "do", "dos",
        "van", "von", "der", "den", "di", "du", "bin", "ibn"
    };

    private static readonly Regex NonAlphaNumeric = new("[^a-z0-9]", RegexOptions.Compiled);
    private static readonly Regex WhitespaceRun = new(@"\s+", RegexOptions.Compiled);

    // Namespace amplio para alias editados por un administrador (permite '.', '_', '-');
    // el generador nunca produce esos caracteres por sí mismo (siempre un subconjunto de
    // ^[a-z0-9]{3,30}$), pero un valor tecleado a mano debe validarse contra este namespace.
    private static readonly Regex AliasFormat = new("^[a-z0-9._-]{3,30}$", RegexOptions.Compiled);

    /// <summary>Genera el alias base (intento 1, sin sufijo de colisión).</summary>
    public static string Generate(string nombre, string email) => Generate(nombre, email, attempt: 1);

    /// <summary>
    /// Genera un candidato de alias para el intento <paramref name="attempt"/> (1-based,
    /// coincide con el "k" del diagrama de secuencia §2.2 de design.md). El intento 1 no
    /// lleva sufijo; desde el intento 2 se agrega el número tal cual (2, 3, ...), recortando
    /// primero el componente de apellido para que el total, sufijo incluido, nunca exceda
    /// <see cref="MaxLength"/> (design.md §1.6 — "truncar primero, sufijo nunca se trunca").
    /// </summary>
    public static string Generate(string nombre, string email, int attempt)
    {
        if (attempt < 1)
            throw new ArgumentOutOfRangeException(nameof(attempt), "El intento debe ser >= 1.");

        var suffix = attempt == 1 ? string.Empty : attempt.ToString(CultureInfo.InvariantCulture);
        var budget = MaxLength - suffix.Length;

        var units = Tokenize(nombre);
        if (units.Count == 0)
        {
            // Cero unidades utilizables en el nombre: cadena de reserva determinista
            // (design.md §1.4, "open item 2") — nunca lanza, nunca aleatorio.
            units = Tokenize(LocalPart(email));
            if (units.Count == 0)
                units = ["user"];
        }

        var (composed, leftover) = Compose(units, budget);
        var candidate = PadToMinimum(composed, leftover);

        return candidate + suffix;
    }

    /// <summary>
    /// Valida el namespace amplio de alias (admin-editable): minúsculas, dígitos, '.', '_',
    /// '-'; 3 a 30 caracteres; nunca '@' — mantiene el namespace de alias provably disjoint
    /// del namespace de email (design.md §1.1, requisito "Alias Format Constraint").
    /// </summary>
    public static bool IsValidAliasFormat(string alias) =>
        !string.IsNullOrEmpty(alias) && AliasFormat.IsMatch(alias) && !alias.Contains('@');

    private static string LocalPart(string email)
    {
        var at = email.IndexOf('@');
        return at < 0 ? email : email[..at];
    }

    // Pipeline de normalización, design.md §1.1 pasos 0-7.
    private static List<string> Tokenize(string input)
    {
        // Paso 0: cortar en el primer '@' (defensivo — AdminUserSeeder usa Nombre = email).
        var at = input.IndexOf('@');
        var cut = at < 0 ? input : input[..at];

        // Pasos 1-2: NFD + descartar marcas diacríticas combinantes (acentos).
        var formD = cut.Normalize(NormalizationForm.FormD);
        var stripped = new StringBuilder(formD.Length);
        foreach (var ch in formD)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                stripped.Append(ch);
        }

        // Paso 3: minúsculas invariantes.
        var lower = stripped.ToString().ToLowerInvariant().Trim();
        if (lower.Length == 0)
            return [];

        // Paso 4: separar solo por corridas de espacio en blanco (nunca por '.' o '-',
        // así "Pérez-León" y "J.R." quedan como un solo token hasta el paso 5).
        var rawTokens = WhitespaceRun.Split(lower);

        // Pasos 5-6: conservar solo [a-z0-9] dentro de cada token, descartar los vacíos.
        var tokens = rawTokens
            .Select(t => NonAlphaNumeric.Replace(t, string.Empty))
            .Where(t => t.Length > 0)
            .ToList();

        // Paso 7: plegar partículas nobiliarias hacia la siguiente unidad.
        return FoldParticles(tokens);
    }

    // design.md §1.2 — cada corrida de partículas consecutivas se concatena (sin
    // separador) sobre la SIGUIENTE unidad que no sea partícula. Una corrida final sin
    // unidad siguiente se pliega hacia atrás sobre la última unidad ya formada; si no hay
    // ninguna unidad previa (el nombre son solo partículas), se descarta -> n = 0.
    private static List<string> FoldParticles(List<string> tokens)
    {
        var units = new List<string>();
        var pending = string.Empty;

        foreach (var token in tokens)
        {
            if (NobiliaryParticles.Contains(token))
            {
                pending += token;
                continue;
            }

            units.Add(pending + token);
            pending = string.Empty;
        }

        if (pending.Length > 0 && units.Count > 0)
            units[^1] += pending;

        return units;
    }

    // Composición (design.md §1.3-1.4): devuelve el alias compuesto y el "material"
    // sobrante -- en el mismo orden que Concat(unidades) -- para que PadToMinimum pueda
    // completar por debajo del mínimo de 3 caracteres sin reutilizar nada ya incluido.
    private static (string Base, string Leftover) Compose(List<string> units, int budget)
    {
        var n = units.Count;

        if (n >= 3)
        {
            // n >= 3: primera unidad = nombre de pila (solo inicial), penúltima = apellido
            // paterno (recortado si hace falta), última = apellido materno (solo inicial).
            // Todo lo demás (unidades intermedias) se ignora por posición estricta, sin
            // heurística semántica (design.md §1.3).
            var initial = units[0][..1];
            var afterInitial = units[0][1..];
            var middleLeftover = string.Concat(units.Skip(1).Take(n - 3));
            var surname = units[n - 2];
            var surnameUsed = Clamp(surname, Math.Max(0, budget - 2));
            var surnameLeftover = surname[surnameUsed.Length..];
            var trailingInitial = units[n - 1][..1];
            var trailingLeftover = units[n - 1][1..];

            var composed = initial + surnameUsed + trailingInitial;
            var leftover = afterInitial + middleLeftover + surnameLeftover + trailingLeftover;
            return (composed, leftover);
        }

        if (n == 2)
        {
            var initial = units[0][..1];
            var afterInitial = units[0][1..];
            var surnameUsed = Clamp(units[1], Math.Max(0, budget - 1));
            var surnameLeftover = units[1][surnameUsed.Length..];

            var composed = initial + surnameUsed;
            var leftover = afterInitial + surnameLeftover;
            return (composed, leftover);
        }

        // n == 1: el token completo, sin inicial (design.md §1.4, "open item 1" — un
        // mononimo ES el identificador de la persona). n == 0 nunca llega hasta acá:
        // Generate ya resolvió la cadena de reserva antes de llamar a Compose.
        var used = Clamp(units[0], budget);
        return (used, units[0][used.Length..]);
    }

    private static string Clamp(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..Math.Max(0, maxLength)];

    // design.md §1.5 — mientras el alias mida menos de MinLength: primero se completa con
    // material del nombre todavía no incorporado (en el mismo orden que Concat(unidades));
    // agotado ese material, se rellena con 'x'. Nunca dígitos (colisionaría visualmente con
    // el sufijo de colisión numérico).
    private static string PadToMinimum(string composed, string leftover)
    {
        if (composed.Length >= MinLength)
            return composed;

        var result = new StringBuilder(composed);
        var i = 0;
        while (result.Length < MinLength && i < leftover.Length)
            result.Append(leftover[i++]);

        while (result.Length < MinLength)
            result.Append('x');

        return result.ToString();
    }
}
