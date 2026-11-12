using System.ComponentModel.DataAnnotations;
using FCG.Application.Requests.Usuarios;

namespace FCG.Application.Tests.Requests;

// Estes atributos são o que o endpoint de cadastro realmente valida (via ModelState).
public class CriarUsuarioRequestTests
{
    private static CriarUsuarioRequest RequestValido() => new()
    {
        Nome = "Maria Silva",
        Email = "maria@fcg.com",
        Senha = "Senha@123",
        ConfirmacaoSenha = "Senha@123",
        DataNascimento = new DateOnly(1990, 5, 10)
    };

    private static List<ValidationResult> Validar(CriarUsuarioRequest request)
    {
        var erros = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), erros, validateAllProperties: true);
        return erros;
    }

    [Fact]
    public void Request_valido_passa()
    {
        Assert.Empty(Validar(RequestValido()));
    }

    [Fact]
    public void Confirmacao_diferente_da_senha_falha()
    {
        var erros = Validar(RequestValido() with { ConfirmacaoSenha = "Outra@123" });

        Assert.Contains(erros, e => e.MemberNames.Contains(nameof(CriarUsuarioRequest.ConfirmacaoSenha)));
    }

    [Fact]
    public void Data_de_nascimento_ausente_falha()
    {
        var erros = Validar(RequestValido() with { DataNascimento = null });

        Assert.Contains(erros, e => e.MemberNames.Contains(nameof(CriarUsuarioRequest.DataNascimento)));
    }

    [Theory]
    [InlineData(nameof(CriarUsuarioRequest.Nome))]
    [InlineData(nameof(CriarUsuarioRequest.Email))]
    [InlineData(nameof(CriarUsuarioRequest.Senha))]
    public void Campos_obrigatorios_ausentes_falham(string campo)
    {
        var request = campo switch
        {
            nameof(CriarUsuarioRequest.Nome) => RequestValido() with { Nome = null! },
            nameof(CriarUsuarioRequest.Email) => RequestValido() with { Email = null! },
            _ => RequestValido() with { Senha = null! }
        };

        Assert.Contains(Validar(request), e => e.MemberNames.Contains(campo));
    }
}
