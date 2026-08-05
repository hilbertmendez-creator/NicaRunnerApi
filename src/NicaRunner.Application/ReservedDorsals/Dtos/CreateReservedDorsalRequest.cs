using System.ComponentModel.DataAnnotations;

namespace NicaRunner.Application.ReservedDorsals.Dtos;

public record CreateReservedDorsalRequest(
    [Required, MaxLength(20)] string Dorsal,
    [MaxLength(500)] string? Motivo);
