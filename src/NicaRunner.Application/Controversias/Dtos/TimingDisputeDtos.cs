using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Controversias.Dtos;

public record TimingDisputeDto(
    int Id,
    int RaceId,
    int? ResultId,
    int? RunnerId,
    string? Dorsal,
    string CorredorNombre,
    double? ChipSeconds,
    double? CheckpointSeconds,
    double? CameraSeconds,
    DisputeEstado Estado,
    string? CapturistaNombre,
    string? CapturaNote,
    int? ResolvedByUserId,
    DateTime? ResolvedAt,
    DateTime UpdatedAt);

// ASSUMPTION: Admin resuelve; Lector RO (HTTP authz en D2).
// Oficial exige confirmApplyOfficial + selectedSource con tiempo no nulo.
public record ResolveDisputeRequest(
    DisputeEstado Estado,
    TimingSource? SelectedSource = null,
    bool ConfirmApplyOfficial = false);
