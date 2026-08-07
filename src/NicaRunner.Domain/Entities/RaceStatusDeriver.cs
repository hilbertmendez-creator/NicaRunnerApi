namespace NicaRunner.Domain.Entities;

/// <summary>
/// Race.Estado deja de ser un dato que alguien setea: se DERIVA del estado de las
/// categorías. Está acá como función pura para poder probar la tabla completa sin
/// construir un grafo de Race, y para que exista un solo lugar donde vive la regla.
/// </summary>
public static class RaceStatusDeriver
{
    public static RaceStatus From(IReadOnlyCollection<RaceCategory> categories)
    {
        if (categories.Count == 0)
            return RaceStatus.Planeada;

        if (categories.All(c => c.Estado == RaceCategoryStatus.Terminada))
            return RaceStatus.Terminada;

        if (categories.All(c => c.Estado == RaceCategoryStatus.Planeada))
            return RaceStatus.Planeada;

        // Queda cualquier mezcla: al menos una salió de Planeada y no todas cerraron.
        return RaceStatus.EnCurso;
    }
}
