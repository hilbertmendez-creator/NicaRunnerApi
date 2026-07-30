namespace NicaRunner.Application.Common;

/// <summary>
/// login-lockout: "Temporary Account Lock" (design.md §3.3) — umbral y duración
/// configuration-driven. <see cref="Threshold"/> = 0 desactiva el lockout sin
/// necesidad de deploy.
/// </summary>
public class LockoutOptions
{
    public int Threshold { get; set; } = 5;
    public int DurationMinutes { get; set; } = 15;
}
