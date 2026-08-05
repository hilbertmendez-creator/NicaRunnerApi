using System.ComponentModel.DataAnnotations;

namespace NicaRunner.Application.Registrations.Dtos;

public record RejectRegistrationRequest(
    [Required, MaxLength(500)] string MotivoRechazo);
