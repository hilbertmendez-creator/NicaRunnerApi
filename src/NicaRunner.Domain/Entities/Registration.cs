namespace NicaRunner.Domain.Entities;

public enum RegistrationStatus
{
    Pendiente,
    ComprobanteSubido,
    Confirmada,
    Rechazada
}

// public-runner-registration-manual-payment: inscripción pública pendiente de revisión
// admin. RunnerId permanece null hasta Confirmada — no existe Runner mientras la
// inscripción no se confirma (design.md D3, reserve-then-compensate).
public class Registration
{
    public int Id { get; set; }
    public int RaceId { get; set; }
    public int RaceCategoryId { get; set; }

    public string Nombre { get; set; } = string.Empty;
    public string? Apellidos { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public Sexo? Sexo { get; set; }
    public string? Club { get; set; }

    // D6: requerido en la sumisión. La edad SIEMPRE se calcula contra Race.FechaCarrera
    // (EdadCalculator.AtRaceDate), nunca DateTime.Now, para que la clasificación de
    // categoría/minoría de edad no cambie después de enviada la inscripción.
    public DateTime FechaNacimiento { get; set; }

    // Requeridos solo cuando la edad calculada a FechaCarrera cae bajo
    // RegistrationOptions:EdadMayoriaEdad (validado en Application, no como constraint de DB).
    public string? ContactoEmergenciaNombre { get; set; }
    public string? ContactoEmergenciaTelefono { get; set; }

    public RegistrationStatus Estado { get; set; } = RegistrationStatus.Pendiente;

    // Comprobante de transferencia bancaria. Rellenar transiciona Pendiente -> ComprobanteSubido.
    public string? ReferenciaTransferencia { get; set; }

    // Snapshot de RaceCategory.Precio al momento de la sumisión — un cambio posterior
    // de precio en la RaceCategory no debe alterar lo ya inscrito.
    public decimal? PrecioAplicado { get; set; }

    // Rellenado únicamente al confirmar (design.md Data Flow, paso 3 — promote).
    public int? RunnerId { get; set; }

    public int? RevisadoPorUserId { get; set; }
    public DateTime? RevisadoAt { get; set; }
    public string? MotivoRechazo { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Race Race { get; set; } = null!;
    public RaceCategory RaceCategory { get; set; } = null!;
    public Runner? Runner { get; set; }
    public User? RevisadoPor { get; set; }
}
