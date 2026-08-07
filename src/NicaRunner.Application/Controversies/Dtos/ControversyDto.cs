namespace NicaRunner.Application.Controversies.Dtos;

public record ControversyDto(
    int Id,
    int RaceId,
    string Dorsal,
    string Nombre,
    string Categoria,
    double? TiempoChip,
    double? TiempoCaptura,
    double? TiempoCamara,
    double? Diferencia,
    string Estado); // Abierta | Resuelta

/// <summary>
/// Estados válidos de una controversia. El "resolver" recibe el estado elegido
/// y lo persiste; hoy el backoffice solo resuelve ("Resuelta"), pero el contrato
/// permite volver a abrir.
/// </summary>
public static class ControversyState
{
    public const string Abierta = "Abierta";
    public const string Resuelta = "Resuelta";

    public static bool IsValid(string? estado) =>
        estado is Abierta or Resuelta;
}