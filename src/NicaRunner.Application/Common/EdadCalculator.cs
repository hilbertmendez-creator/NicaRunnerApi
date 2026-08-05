namespace NicaRunner.Application.Common;

// Extraído de RunnerService.CalculateEdad (privado) para que RegistrationService
// (public-runner-registration-manual-payment) valide la misma regla de edad-por-categoría
// en la sumisión pública que RunnerService ya aplicaba en la creación manual/import Excel.
// Misma lógica exacta: años completos cumplidos entre fechaNacimiento y asOf — asOf es
// SIEMPRE Race.FechaCarrera, nunca DateTime.Now, para que la clasificación no cambie con
// el paso del tiempo entre la sumisión y la revisión/confirmación. Nunca negativa.
public static class EdadCalculator
{
    public static int AtRaceDate(DateTime fechaNacimiento, DateTime asOf)
    {
        var edad = asOf.Year - fechaNacimiento.Year;
        if (fechaNacimiento.Date > asOf.Date.AddYears(-edad))
            edad--;
        return Math.Max(edad, 0);
    }
}
