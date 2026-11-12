using FCG.Application.Requests.Usuarios;
using FCG.Application.Validators.Usuarios;

namespace FCG.Application.Tests.Validators;

public class LoginUsuarioValidatorTests
{
    private readonly LoginUsuarioValidator _validator = new();

    [Fact]
    public void Login_valido_passa()
    {
        var resultado = _validator.Validate(new LoginUsuarioRequest { Email = "maria@fcg.com", Senha = "qualquer" });

        Assert.True(resultado.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("sem-arroba")]
    public void Email_vazio_ou_malformado_falha(string email)
    {
        var resultado = _validator.Validate(new LoginUsuarioRequest { Email = email, Senha = "qualquer" });

        Assert.Contains(resultado.Errors, e => e.PropertyName == nameof(LoginUsuarioRequest.Email));
    }

    [Fact]
    public void Senha_vazia_falha()
    {
        var resultado = _validator.Validate(new LoginUsuarioRequest { Email = "maria@fcg.com", Senha = "" });

        Assert.Contains(resultado.Errors, e => e.PropertyName == nameof(LoginUsuarioRequest.Senha));
    }
}
