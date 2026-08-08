using System.ComponentModel.DataAnnotations;

namespace NicaRunner.Application.Results.Dtos;

public record VoidResultRequest([Required, MinLength(1)] string Razon);
