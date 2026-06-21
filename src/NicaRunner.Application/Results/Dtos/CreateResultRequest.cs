using System.ComponentModel.DataAnnotations;

namespace NicaRunner.Application.Results.Dtos;

public record CreateResultRequest(
    [Required, MaxLength(20)] string Dorsal,
    [Required] DateTime TiempoLlegada);
