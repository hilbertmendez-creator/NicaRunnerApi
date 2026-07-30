using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace NicaRunner.Application.Common.Validation;

// user-auth: nivel de seguridad mínimo para cualquier contraseña nueva (reset
// y change-password comparten esta política) — mínimo 8 caracteres con al
// menos una mayúscula, una minúscula, un dígito y un símbolo.
public partial class StrongPasswordAttribute : ValidationAttribute
{
    public StrongPasswordAttribute()
        : base("La contraseña debe tener al menos 8 caracteres e incluir mayúscula, minúscula, número y símbolo.")
    {
    }

    public override bool IsValid(object? value) =>
        value is string password && PasswordPolicyRegex().IsMatch(password);

    [GeneratedRegex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).{8,}$")]
    private static partial Regex PasswordPolicyRegex();
}
