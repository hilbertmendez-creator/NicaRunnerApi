using System.ComponentModel.DataAnnotations;

namespace NicaRunner.Application.PublicResults.Dtos;

public record CreatePublicTokenRequest(
    [Range(1, 365)] int DiasExpiracion = 30);
