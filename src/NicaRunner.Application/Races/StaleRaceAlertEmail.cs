using NicaRunner.Application.Races.Dtos;

namespace NicaRunner.Application.Races;

/// <summary>
/// Aviso texto-plano a los Administradores cuando el barrido anti-zombie encuentra
/// carreras EnCurso sin actividad de captura reciente. Solo texto, mismo criterio que
/// RaceClosedAdminEmail — el template HTML es el seam de PR 3.
/// </summary>
public static class StaleRaceAlertEmail
{
    public static (string Subject, string Body) Build(List<StaleRaceDto> staleRaces)
    {
        var subject = staleRaces.Count == 1
            ? "Carrera sin actividad: revisá si sigue en curso"
            : $"{staleRaces.Count} carreras sin actividad: revisá si siguen en curso";

        var lines = staleRaces.Select(r =>
            $"- \"{r.Nombre}\" (id {r.RaceId}): sin capturas desde {r.LastActivityUtc:u} UTC.");

        var body =
            "El barrido automático encontró carreras EnCurso sin actividad de captura " +
            "reciente. Ninguna se cerró sola — revisá cada una y cerrala manualmente si " +
            "ya terminó, o seguí capturando si sigue activa.\n\n" +
            string.Join("\n", lines);

        return (subject, body);
    }
}
