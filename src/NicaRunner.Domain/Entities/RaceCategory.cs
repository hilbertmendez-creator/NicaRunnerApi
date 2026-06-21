namespace NicaRunner.Domain.Entities;

public class RaceCategory
{
    public int Id { get; set; }
    public int RaceId { get; set; }
    public string NombreCategoria { get; set; } = string.Empty;
    public decimal Distancia { get; set; } // km
    public int EdadMinima { get; set; }
    public int EdadMaxima { get; set; }
    public int Orden { get; set; }

    public Race Race { get; set; } = null!;
    public ICollection<Runner> Runners { get; set; } = new List<Runner>();
}
