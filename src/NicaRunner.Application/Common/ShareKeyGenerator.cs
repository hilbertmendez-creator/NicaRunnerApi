using System.Security.Cryptography;

namespace NicaRunner.Application.Common;

/// <summary>
/// Generador puro de claves opacas para enlaces públicos permanentes (design.md
/// Decisión 1). 128 bits de entropía vía <see cref="RandomNumberGenerator"/> (CSPRNG),
/// codificados Base64Url: 22 caracteres, sin padding, alfabeto `[A-Za-z0-9_-]` —
/// seguro para ir directo en una URL sin escapar. Deliberadamente NO usa
/// <see cref="Guid.NewGuid"/>: Guid documenta unicidad, no impredecibilidad, y este
/// valor se expone públicamente como identificador de acceso.
/// </summary>
public static class ShareKeyGenerator
{
    private const int KeyLengthBytes = 16;

    public static string Generate()
    {
        var bytes = RandomNumberGenerator.GetBytes(KeyLengthBytes);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
