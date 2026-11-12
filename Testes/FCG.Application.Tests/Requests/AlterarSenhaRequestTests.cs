using System.ComponentModel.DataAnnotations;

namespace FCG.Application.Tests.Requests;

// A alteração de senha não tem validator do FluentValidation: as regras são atributos do próprio request.
public class AlterarSenhaRequestTests
{
    private static AlterarSenhaRequest RequestValido() => new()
    {
        Email = "maria@fcg.com",
        SenhaAtual = "Senha@123",
        NovaSenha = "NovaSenha@456",
        ConfirmarNovaSenha = "NovaSenha@456"
    };

    private static List<ValidationResult> Validar(AlterarSenhaRequest request)
    {
        var erros = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), erros, validateAllProperties: true);
        return erros;
    }

    private static bool TemErroEm(List<ValidationResult> erros, string propriedade)
        => erros.Any(e => e.MemberNames.Contains(propriedade));

    [Fact]
    public void Request_valido_passa()
    {
        Assert.Empty(Validar(RequestValido()));
    }

    [Theory]
    [InlineData("")]
    [InlineData("sem-arroba")]
    public void Email_vazio_ou_invalido_falha(string email)
    {
        var erros = Validar(RequestValido() with { Email = email });

        Assert.True(TemErroEm(erros, nameof(AlterarSenhaRequest.Email)));
    }

    [Fact]
    public void Senha_atual_vazia_falha()
    {
        var erros = Validar(RequestValido() with { SenhaAtual = "" });

        Assert.True(TemErroEm(erros, nameof(AlterarSenhaRequest.SenhaAtual)));
    }

    [Fact]
    public void Nova_senha_com_menos_de_8_caracteres_falha()
    {
        var erros = Validar(RequestValido() with { NovaSenha = "Ab@1", ConfirmarNovaSenha = "Ab@1" });

        Assert.True(TemErroEm(erros, nameof(AlterarSenhaRequest.NovaSenha)));
    }

    [Fact]
    public void Confirmacao_diferente_da_nova_senha_falha()
    {
        var erros = Validar(RequestValido() with { ConfirmarNovaSenha = "OutraSenha@789" });

        Assert.True(TemErroEm(erros, nameof(AlterarSenhaRequest.ConfirmarNovaSenha)));
    }

    [Fact]
    public void Confirmacao_vazia_falha()
    {
        var erros = Validar(RequestValido() with { ConfirmarNovaSenha = "" });

        Assert.True(TemErroEm(erros, nameof(AlterarSenhaRequest.ConfirmarNovaSenha)));
    }
}
