namespace NicaRunner.Application.Categories.Dtos;

public record RaceCategoryDto(
    int CategoryId,
    string Codigo,
    string NombreCategoria,
    string? Descripcion,
    decimal Distancia,
    int EdadMinima,
    int EdadMaxima,
    int Orden,
    // public-runner-registration-manual-payment (tasks.md 2.11): appended with defaults
    // so existing positional construction (AssignAsync/GetAllByRaceAsync, which have no
    // RaceCategory row in hand) keeps compiling and reports "not configured yet".
    int? Capacidad = null,
    decimal? Precio = null,
    int ConfirmedCount = 0);
