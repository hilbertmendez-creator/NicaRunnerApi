namespace NicaRunner.Domain.Entities;

// Asociación: qué categorías del catálogo global (Category) participan en una carrera.
public class RaceCategory
{
    public int Id { get; set; }
    public int RaceId { get; set; }
    public int CategoryId { get; set; }

    // public-runner-registration-manual-payment (design.md D1): capacidad y precio viven
    // acá, no en Category (catálogo global) — 5K y 10K de la misma carrera venden distinto.
    // Capacidad null = sin límite. ConfirmedCount es el contador denormalizado que
    // TryReserveSlotAsync incrementa atómicamente (D2); nunca se escribe desde otro lado.
    public int? Capacidad { get; set; }
    public decimal? Precio { get; set; }
    public int ConfirmedCount { get; set; }

    public Race Race { get; set; } = null!;
    public Category Category { get; set; } = null!;
}
