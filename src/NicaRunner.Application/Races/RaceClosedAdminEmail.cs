namespace NicaRunner.Application.Races;

/// <summary>
/// Aviso texto-plano a los Administradores cuando un Capturista cierra una carrera
/// (decisión 8: TODOS los Administradores, no solo Race.AdminId). Solo texto — el
/// template HTML es el seam de PR 3 (design.md D6).
/// </summary>
public static class RaceClosedAdminEmail
{
    public static (string Subject, string Body) Build(string raceName, string closedByName)
    {
        var subject = $"Carrera cerrada: {raceName}";
        var body =
            $"La carrera \"{raceName}\" fue cerrada por {closedByName} (Capturista). " +
            "Podés revisar los resultados finales y la bitácora en el backoffice.";

        return (subject, body);
    }
}
