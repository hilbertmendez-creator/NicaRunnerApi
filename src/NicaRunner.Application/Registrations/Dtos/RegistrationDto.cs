using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Registrations.Dtos;

public record RegistrationDto(
    int Id,
    int RaceId,
    int RaceCategoryId,
    string Nombre,
    string? Apellidos,
    string? Telefono,
    string? Email,
    Sexo? Sexo,
    string? Club,
    DateTime FechaNacimiento,
    string? ContactoEmergenciaNombre,
    string? ContactoEmergenciaTelefono,
    RegistrationStatus Estado,
    string? ReferenciaTransferencia,
    decimal? PrecioAplicado,
    int? RunnerId,
    int? RevisadoPorUserId,
    DateTime? RevisadoAt,
    string? MotivoRechazo,
    DateTime CreatedAt);
