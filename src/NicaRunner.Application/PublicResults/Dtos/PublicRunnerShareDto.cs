namespace NicaRunner.Application.PublicResults.Dtos;

/// <summary>
/// design.md Decisión 7: modelo de lectura del enlace público de detalle del corredor
/// (GET /api/public/corredor/{shareKey}). Nullable-honesto por diseño — sin ceros
/// centinela ni strings "--": null significa explícitamente "todavía no hay dato".
/// <see cref="Nombre"/>, <see cref="Sexo"/> y <see cref="Edad"/> del corredor NO
/// aparecen acá — ver spec.md "Public Field Whitelist" — este DTO ES el límite de
/// privacidad y nunca debe ampliarse hacia la entidad completa.
/// </summary>
public record PublicRunnerShareDto(
    string RaceName,
    string Slogan,
    DateTime FechaCarrera,
    string Nombre,
    string? Apellidos,
    string? Club,
    string Dorsal,
    string NombreCategoria,
    decimal Distancia,
    DateTime? TiempoLlegadaUtc,
    int? TiempoTranscurridoSegundos,
    int? PosicionCategoria,
    int? TotalCategoria,
    int? PosicionGeneral,
    int? TotalGeneral);
