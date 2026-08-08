namespace NicaRunner.Application.Results.Dtos;

/// <summary>Un conflicto abierto, con todos los resultados peleando por el mismo dorsal.</summary>
public record DisputeGroupDto(
    int DisputeGroupId,
    int RaceId,
    NicaRunner.Domain.Entities.DisputeMotivo Motivo,
    string? DorsalEnDisputa,
    DateTime AbiertaUtc,
    List<ResultDto> Resultados);
