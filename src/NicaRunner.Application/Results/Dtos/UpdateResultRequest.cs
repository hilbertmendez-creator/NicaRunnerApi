using System.ComponentModel.DataAnnotations;

namespace NicaRunner.Application.Results.Dtos;

public record UpdateResultRequest(
    [Required, MaxLength(20)] string Dorsal,
    [Required] DateTime TiempoLlegada,
    [Required, MinLength(3), MaxLength(300)] string Razon);
