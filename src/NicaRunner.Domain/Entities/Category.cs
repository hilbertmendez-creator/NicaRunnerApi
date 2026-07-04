namespace NicaRunner.Domain.Entities;

public class Category
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string NombreCategoria { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal Distancia { get; set; } // km
    public int EdadMinima { get; set; }
    public int EdadMaxima { get; set; }
    public int Orden { get; set; }

    public ICollection<RaceCategory> RaceCategories { get; set; } = new List<RaceCategory>();
    public ICollection<Runner> Runners { get; set; } = new List<Runner>();
}
