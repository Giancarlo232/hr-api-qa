namespace HrApi.Tests;

/// <summary>Pruebas unitarias puras (sin HTTP ni base de datos).</summary>
public class ValidatorsTests
{
    [Theory]
    [Trait("Caso", "TC-UNI-01")]
    [InlineData("persona@correo.com")]
    [InlineData("a.b-c@dominio.gt")]
    public void Email_valido_devuelve_true(string email) => Assert.True(Validators.Email(email));

    [Theory]
    [Trait("Caso", "TC-UNI-02")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("sin-arroba")]
    [InlineData("a@b")]
    [InlineData("Nombre <a@b.com>")]
    public void Email_invalido_devuelve_false(string? email) => Assert.False(Validators.Email(email));
}
