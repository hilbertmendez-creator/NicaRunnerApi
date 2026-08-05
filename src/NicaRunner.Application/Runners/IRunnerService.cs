using NicaRunner.Application.Runners.Dtos;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Runners;

public interface IRunnerService
{
    Task<RunnerDto> CreateAsync(int raceId, CreateRunnerRequest request, CancellationToken ct = default);
    Task<List<RunnerDto>> GetAllByRaceAsync(int raceId, CancellationToken ct = default);
    Task<RunnerDto> GetByIdAsync(int raceId, int runnerId, CancellationToken ct = default);
    Task<RunnerDto> UpdateAsync(int raceId, int runnerId, UpdateRunnerRequest request, CancellationToken ct = default);
    Task DeleteAsync(int raceId, int runnerId, CancellationToken ct = default);
    Task<byte[]> GenerateImportTemplateAsync(int raceId, CancellationToken ct = default);
    Task<ImportRunnersResultDto> ImportFromExcelAsync(int raceId, Stream excelStream, CancellationToken ct = default);

    /// <summary>
    /// public-runner-registration-manual-payment (design.md D3/Data Flow, paso "promote"):
    /// única autoridad de creación de Runner a partir de una Registration confirmada, con
    /// el Dorsal siempre admin-supplied (D7 — nunca generado). A diferencia de los demás
    /// métodos de esta interfaz, devuelve la ENTIDAD de dominio (no un Dto) y NO llama
    /// SaveChangesAsync: solo agrega el Runner al DbContext compartido. Esto es
    /// deliberado — RegistrationService necesita enlazar `Registration.Runner = runner`
    /// (fixup de FK vía navegación, antes de que el Runner tenga un Id real) y confirmar
    /// runner + Registration.RunnerId + AuditLog en un solo SaveChangesAsync, para que un
    /// fallo posterior a la promoción nunca deje un Runner huérfano persistido mientras se
    /// compensan los pasos de claim/reserve (ver RegistrationService.ConfirmAsync).
    /// </summary>
    Task<Runner> CreateFromRegistrationAsync(Registration registration, string dorsal, CancellationToken ct = default);
}
