using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Categories.Dtos;

// Los 8 primeros campos son el contrato existente que ya consume el backoffice:
// no reordenar ni renombrar. Lo nuevo se agrega al final.
public record RaceCategoryDto(
    int CategoryId,
    string Codigo,
    string NombreCategoria,
    string? Descripcion,
    decimal Distancia,
    int EdadMinima,
    int EdadMaxima,
    int Orden,
    RaceCategoryStatus Estado = RaceCategoryStatus.Planeada,
    DateTime? StartUtc = null,
    int? StartedByUserId = null,
    string? StartedByNombre = null,
    DateTime? ClosedUtc = null,
    int? ClosedByUserId = null,
    string? ClosedByNombre = null);
