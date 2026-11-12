using FCG.Domain.Entities;
using FCG.Domain.Entities.Validations;

namespace FCG.Domain.Tests.Entities;

public class UsuarioValidationTests
{
    private readonly UsuarioValidation _validator = new();

    private static UsuarioEntity UsuarioValido() => new()
    {
        Nome = "Maria Silva",
        Email = "maria@fcg.com",
        DataNascimento = new DateOnly(1990, 5, 10),
        IdentityUserId = "identity-123"
    };

    [Fact]
    public void Usuario_valido_passa()
    {
        var resultado = _validator.Validate(UsuarioValido());

        Assert.True(resultado.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("A")]
    public void Nome_vazio_ou_curto_demais_falha(string nome)
    {
        var usuario = UsuarioValido();
        usuario.Nome = nome;

        var resultado = _validator.Validate(usuario);

        Assert.Contains(resultado.Errors, e => e.PropertyName == nameof(UsuarioEntity.Nome));
    }

    [Fact]
    public void Nome_com_mais_de_100_caracteres_falha()
    {
        var usuario = UsuarioValido();
        usuario.Nome = new string('a', 101);

        var resultado = _validator.Validate(usuario);

        Assert.Contains(resultado.Errors, e => e.PropertyName == nameof(UsuarioEntity.Nome));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(100)]
    public void Nome_nos_limites_do_tamanho_passa(int tamanho)
    {
        var usuario = UsuarioValido();
        usuario.Nome = new string('a', tamanho);

        Assert.True(_validator.Validate(usuario).IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("sem-arroba")]
    [InlineData("@fcg.com")]
    public void Email_vazio_ou_malformado_falha(string email)
    {
        var usuario = UsuarioValido();
        usuario.Email = email;

        var resultado = _validator.Validate(usuario);

        Assert.Contains(resultado.Errors, e => e.PropertyName == nameof(UsuarioEntity.Email));
    }

    [Fact]
    public void Data_de_nascimento_nao_informada_falha()
    {
        var usuario = UsuarioValido();
        usuario.DataNascimento = default;

        var resultado = _validator.Validate(usuario);

        Assert.Contains(resultado.Errors, e => e.PropertyName == nameof(UsuarioEntity.DataNascimento));
    }

    [Fact]
    public void Data_de_nascimento_no_futuro_falha()
    {
        var usuario = UsuarioValido();
        usuario.DataNascimento = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

        var resultado = _validator.Validate(usuario);

        Assert.Contains(resultado.Errors, e => e.PropertyName == nameof(UsuarioEntity.DataNascimento));
    }

    [Fact]
    public void Vinculo_com_usuario_de_autenticacao_vazio_falha()
    {
        var usuario = UsuarioValido();
        usuario.IdentityUserId = "";

        var resultado = _validator.Validate(usuario);

        Assert.Contains(resultado.Errors, e => e.PropertyName == nameof(UsuarioEntity.IdentityUserId));
    }
}
