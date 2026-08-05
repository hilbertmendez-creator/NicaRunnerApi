using System.ComponentModel.DataAnnotations;

namespace NicaRunner.Application.Registrations.Dtos;

public record UploadReceiptRequest(
    [Required, MaxLength(100)] string ReferenciaTransferencia);
