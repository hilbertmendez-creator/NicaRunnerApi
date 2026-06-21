namespace NicaRunner.Application.Dashboard.Dtos;

public record CategoryProgressDto(
    int CategoryId,
    string NombreCategoria,
    int Inscritos,
    int ConTiempo,
    int Pendientes);
