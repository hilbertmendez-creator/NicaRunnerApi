using System.ComponentModel.DataAnnotations;

namespace NicaRunner.Application.Races.Dtos;

public record JoinByCodeRequest(
    [Required, MinLength(4), MaxLength(12)] string Code);
