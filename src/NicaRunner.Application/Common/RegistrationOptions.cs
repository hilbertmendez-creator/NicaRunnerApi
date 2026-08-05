namespace NicaRunner.Application.Common;

/// <summary>
/// public-runner-registration-manual-payment (design.md D5): umbral de minoría de edad
/// para exigir contacto de emergencia en la sumisión pública. Configuration-driven
/// (sección "RegistrationOptions" en appsettings) para poder ajustar sin recompilar —
/// el binding completo en appsettings*.json llega en Phase 4 (tasks.md 4.2); esta clase
/// y su registro en Program.cs se adelantan acá para que RegistrationService pueda
/// resolver <see cref="EdadMayoriaEdad"/> vía IOptions ya en Phase 2, con el mismo
/// default (18) documentado en design.md.
/// </summary>
public class RegistrationOptions
{
    public int EdadMayoriaEdad { get; set; } = 18;
}
