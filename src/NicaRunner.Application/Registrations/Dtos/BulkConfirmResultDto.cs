namespace NicaRunner.Application.Registrations.Dtos;

// Mirrors NicaRunner.Application.Runners.Dtos.ImportRunnersResultDto — same
// total/processed/errors partial-success shape (design.md D13, "Interfaces / Contracts").
public record BulkConfirmResultDto(
    int TotalFilas,
    int Confirmadas,
    List<ConfirmRowError> Errores);
