namespace NicaRunner.Application.Runners.Dtos;

public record ImportRunnersResultDto(
    int TotalFilas,
    int Importados,
    List<ImportRunnerError> Errores);
