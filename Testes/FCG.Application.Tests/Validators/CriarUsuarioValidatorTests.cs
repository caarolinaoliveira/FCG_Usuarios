using FCG.Application.Requests.Usuarios;
using FCG.Application.Validators.Usuarios;
using FluentValidation.Results;

namespace FCG.Application.Tests.Validators;

public class CriarUsuarioValidatorTests
{
    private readonly CriarUsuarioValidator _validator = new();

    private static CriarUsuarioRequest RequestValido() => new()
    {
        Nome = "Maria Silva",
        Email = "maria@fcg.com",
        Senha = "Senha@123",
        ConfirmacaoSenha = "Senha@123",
        DataNascimento = new DateOnly(1990, 5, 10)
    };

    private static bool TemErroEm(ValidationResult resultado, string propriedade)
        => resultado.Errors.Any(e => e.PropertyName == propriedade);

    [Fact]
    public void Request_valido_passa()
    {
        Assert.True(_validator.Validate(RequestValido()).IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("ab")]
    public void Nome_vazio_ou_com_menos_de_3_caracteres_falha(string nome)
    {
        var resultado = _validator.Validate(RequestValido() with { Nome = nome });

        Assert.True(TemErroEm(resultado, nameof(CriarUsuarioRequest.Nome)));
    }

    [Fact]
    public void Nome_com_mais_de_100_caracteres_falha()
    {
        var resultado = _validator.Validate(RequestValido() with { Nome = new string('a', 101) });

        Assert.True(TemErroEm(resultado, nameof(CriarUsuarioRequest.Nome)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("sem-arroba")]
    [InlineData("maria@")]
    public void Email_vazio_ou_malformado_falha(string email)
    {
        var resultado = _validator.Validate(RequestValido() with { Email = email });

        Assert.True(TemErroEm(resultado, nameof(CriarUsuarioRequest.Email)));
    }

    [Theory]
    [InlineData("", "obrigatória")]
    [InlineData("Ab@1", "Mínimo de 8")]
    [InlineData("senha@123", "maiúscula")]
    [InlineData("SENHA@123", "minúscula")]
    [InlineData("Senha@abc", "número")]
    [InlineData("Senha1234", "especial")]
    public void Senha_fora_da_politica_falha_com_a_mensagem_da_regra(string senha, string trechoDaMensagem)
    {
        var resultado = _validator.Validate(RequestValido() with { Senha = senha, ConfirmacaoSenha = senha });

        Assert.Contains(resultado.Errors,
            e => e.PropertyName == nameof(CriarUsuarioRequest.Senha) && e.ErrorMessage.Contains(trechoDaMensagem));
    }

    [Fact]
    public void Data_de_nascimento_nao_informada_falha()
    {
        var resultado = _validator.Validate(RequestValido() with { DataNascimento = null });

        Assert.True(TemErroEm(resultado, nameof(CriarUsuarioRequest.DataNascimento)));
    }

    [Fact]
    public void Usuario_com_menos_de_18_anos_falha()
    {
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);

        var resultado = _validator.Validate(RequestValido() with { DataNascimento = hoje.AddYears(-18).AddDays(1) });

        Assert.True(TemErroEm(resultado, nameof(CriarUsuarioRequest.DataNascimento)));
    }

    [Fact]
    public void Usuario_que_faz_18_anos_hoje_passa()
    {
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);

        var resultado = _validator.Validate(RequestValido() with { DataNascimento = hoje.AddYears(-18) });

        Assert.True(resultado.IsValid);
    }
}
