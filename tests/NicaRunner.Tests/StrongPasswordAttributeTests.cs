using NicaRunner.Application.Common.Validation;

namespace NicaRunner.Tests;

public class StrongPasswordAttributeTests
{
    private readonly StrongPasswordAttribute _attribute = new();

    [Theory]
    [InlineData("Segura#123")]
    [InlineData("Otra-Clave9")]
    [InlineData("C0ntraseña!Fuerte")]
    public void IsValid_ContraseñaConMayusculaMinusculaNumeroYSimbolo_True(string password) =>
        Assert.True(_attribute.IsValid(password));

    [Theory]
    [InlineData("segura12!")] // sin mayúscula
    [InlineData("SEGURA12!")] // sin minúscula
    [InlineData("Segura!!!")] // sin dígito
    [InlineData("Segura123")] // sin símbolo
    [InlineData("Se1!")] // menos de 8 caracteres
    public void IsValid_ContraseñaSinAlgunRequisito_False(string password) =>
        Assert.False(_attribute.IsValid(password));

    [Fact]
    public void IsValid_ValorNoString_False() =>
        Assert.False(_attribute.IsValid(12345678));
}
