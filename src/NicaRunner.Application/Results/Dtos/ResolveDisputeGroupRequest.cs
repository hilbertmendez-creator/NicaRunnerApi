using System.ComponentModel.DataAnnotations;

namespace NicaRunner.Application.Results.Dtos;

public record DisputeAssignment(int ResultId, string? Dorsal);

/// <summary>
/// F5 del spec, acotado a DorsalDuplicado en este PR — CategoriaSinSalida/
/// CategoriaCerrada se resuelven corrigiendo el StartUtc de la categoría, una
/// capacidad que todavía no existe (ver el PR que agrega esa corrección).
/// </summary>
public record ResolveDisputeGroupRequest(
    List<DisputeAssignment> Asignaciones,
    List<int> Anular,
    [Required, MinLength(1)] string Razon);
