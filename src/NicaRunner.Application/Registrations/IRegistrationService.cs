using NicaRunner.Application.Registrations.Dtos;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Registrations;

public interface IRegistrationService
{
    Task<RegistrationLinkInfoDto> GetLinkInfoAsync(string token, CancellationToken ct = default);
    Task<RegistrationDto> SubmitAsync(string token, SubmitRegistrationRequest request, CancellationToken ct = default);
    Task<RegistrationDto> UploadReceiptAsync(string token, int registrationId, UploadReceiptRequest request, CancellationToken ct = default);
    Task<List<RegistrationDto>> GetAllForReviewAsync(int raceId, RegistrationStatus? estado, CancellationToken ct = default);
    Task<RegistrationDto> ConfirmAsync(int raceId, int registrationId, ConfirmRegistrationRequest request, int adminId, CancellationToken ct = default);
    Task<RegistrationDto> RejectAsync(int raceId, int registrationId, RejectRegistrationRequest request, int adminId, CancellationToken ct = default);

    // registration-review spec.md "Registration Link Administration" (tasks.md 2.12).
    Task<RegistrationLinkDto> CreateLinkAsync(int raceId, CreateRegistrationLinkRequest request, int creatorId, CancellationToken ct = default);
    Task<List<RegistrationLinkDto>> GetAllLinksAsync(int raceId, CancellationToken ct = default);
    Task RevokeLinkAsync(int raceId, int linkId, CancellationToken ct = default);

    // registration-review spec.md "Bulk Confirm via Excel Template" (tasks.md 3.4/3.5).
    Task<byte[]> GenerateBulkConfirmTemplateAsync(int raceId, CancellationToken ct = default);
    Task<BulkConfirmResultDto> ConfirmBulkAsync(int raceId, Stream excelStream, int adminId, CancellationToken ct = default);
}
