using System.ComponentModel.DataAnnotations;

namespace NicaRunner.Application.Controversies.Dtos;

public record ResolveControversyRequest(
    [Required, MaxLength(20)] string Estado);