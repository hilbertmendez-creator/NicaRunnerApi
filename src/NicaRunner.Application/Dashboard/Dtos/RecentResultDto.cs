namespace NicaRunner.Application.Dashboard.Dtos;

public record RecentResultDto(
    int ResultId,
    string Dorsal,
    string Nombre,
    DateTime TiempoLlegada,
    int Posicion,
    string NombreCategoria,
    int CapturistaId);
