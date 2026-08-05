namespace NicaRunner.Domain.Entities;

// design.md D8/D9/D11: bloquea un Dorsal para que ningún camino de escritura (confirm
// individual, confirm bulk, o el RunnerService.CreateAsync/UpdateAsync manual preexistente)
// lo consuma por accidente. La unicidad corre sobre DorsalNormalizado, no sobre el string
// crudo — "21K7" y "21K007" deben bloquear igual. Liberar la reserva es un DELETE explícito
// (ReservedDorsalsController), no automático.
public class ReservedDorsal
{
    public int Id { get; set; }
    public int RaceId { get; set; }
    public string Dorsal { get; set; } = string.Empty;
    public string DorsalNormalizado { get; set; } = string.Empty;
    public string? Motivo { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Race Race { get; set; } = null!;
    public User Creator { get; set; } = null!;
}
