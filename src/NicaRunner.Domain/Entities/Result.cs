namespace NicaRunner.Domain.Entities;

public class Result
{
    public int Id { get; set; }
    public int RaceId { get; set; }
    public int? RunnerId { get; set; }
    public string? Dorsal { get; set; } // null hasta que se asigna; copiado al momento de asignación
    public DateTime TiempoLlegada { get; set; }
    public int Posicion { get; set; }
    public int? CategoryId { get; set; } // null hasta que el dorsal asignado resuelve la categoría
    public ResultEstado Estado { get; set; } = ResultEstado.Valido;

    // Intención, nunca aplicada: si Estado=Controversia por DorsalDuplicado, este es el
    // dorsal que ESTE juez tipeó — Dorsal/CategoryId del registro quedan como estaban.
    public string? DorsalPropuesto { get; set; }

    // Ata los dos lados de un DorsalDuplicado. Null para CategoriaSinSalida/
    // CategoriaCerrada — ahí no hay una "otra parte", el conflicto es contra el estado
    // de la categoría, no contra otro resultado.
    public int? DisputeGroupId { get; set; }
    public DisputeMotivo? DisputeMotivo { get; set; }
    public int CapturistaId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Opcional. Si el cliente (típico: app móvil del capturista en zona con
    /// señal mala) envía el header Idempotency-Key, se persiste acá y se
    /// usa como UK junto con RaceId. Reintentos del mismo POST con el mismo
    /// key devuelven el Result existente en vez de crear uno nuevo, evitando
    /// capturas duplicadas cuando el response original se perdió en la red.
    /// Null para POSTs legacy sin el header — comportamiento backward compat.
    /// </summary>
    public string? IdempotencyKey { get; set; }

    public Race Race { get; set; } = null!;
    public Runner? Runner { get; set; }
    public Category? Category { get; set; }
    public User Capturista { get; set; } = null!;
    public ICollection<ResultAudit> AuditEntries { get; set; } = new List<ResultAudit>();
}
