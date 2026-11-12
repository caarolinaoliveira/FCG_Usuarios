using System.Text;
using FCG.Application.Configuration;
using FCG.Application.Services.Autenticacao;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace FCG.Application.Tests.Services;

public class TokenServiceTests
{
    private const string Segredo = "segredo-de-teste-com-mais-de-32-caracteres!";

    private static TokenService CriarServico(int expiracaoHoras = 2)
        => new(Options.Create(new JwtSettings
        {
            Segredo = Segredo,
            Emissor = "FCG",
            Audiencia = "FCG.Client",
            ExpiracaoHoras = expiracaoHoras
        }));

    // JsonWebTokenHandler é o que o JwtBearer do .NET 8 usa; o JwtSecurityTokenHandler 7.x perde claims ao ler tokens com o Tokens 8.x.
    private static JsonWebToken Ler(string token) => new JsonWebTokenHandler().ReadJsonWebToken(token);

    [Fact]
    public void Token_traz_o_email_do_usuario()
    {
        var token = Ler(CriarServico().GerarToken("maria@fcg.com", new[] { "Usuario" }));

        Assert.Equal("maria@fcg.com", token.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Email).Value);
    }

    [Fact]
    public void Token_traz_a_role_do_usuario()
    {
        var token = Ler(CriarServico().GerarToken("maria@fcg.com", new[] { "Admin" }));

        Assert.Equal("Admin", token.Claims.Single(c => c.Type == "role").Value);
    }

    [Fact]
    public void Token_traz_todas_as_roles_quando_ha_mais_de_uma()
    {
        var token = Ler(CriarServico().GerarToken("maria@fcg.com", new[] { "Usuario", "Admin" }));

        var roles = token.Claims.Where(c => c.Type == "role").Select(c => c.Value);

        Assert.Equal(new[] { "Admin", "Usuario" }, roles.OrderBy(r => r));
    }

    [Fact]
    public void Token_sem_roles_nao_tem_claim_de_role()
    {
        var token = Ler(CriarServico().GerarToken("maria@fcg.com", Array.Empty<string>()));

        Assert.DoesNotContain(token.Claims, c => c.Type == "role");
    }

    [Fact]
    public void Token_traz_emissor_e_audiencia_configurados()
    {
        var token = Ler(CriarServico().GerarToken("maria@fcg.com", new[] { "Usuario" }));

        Assert.Equal("FCG", token.Issuer);
        Assert.Equal(new[] { "FCG.Client" }, token.Audiences);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(24)]
    public void Token_expira_apos_as_horas_configuradas(int horas)
    {
        var antes = DateTime.UtcNow;

        var token = Ler(CriarServico(horas).GerarToken("maria@fcg.com", new[] { "Usuario" }));

        var tolerancia = TimeSpan.FromMinutes(1);
        Assert.InRange(token.ValidTo, antes.AddHours(horas) - tolerancia, DateTime.UtcNow.AddHours(horas) + tolerancia);
    }

    [Fact]
    public async Task Token_e_aceito_pela_validacao_que_a_api_usa()
    {
        var jwt = CriarServico().GerarToken("maria@fcg.com", new[] { "Usuario" });

        var resultado = await new JsonWebTokenHandler().ValidateTokenAsync(jwt, new TokenValidationParameters
        {
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Segredo)),
            ValidateIssuerSigningKey = true,
            ValidIssuer = "FCG",
            ValidAudience = "FCG.Client"
        });

        Assert.True(resultado.IsValid);
    }

    [Fact]
    public async Task Token_assinado_com_outro_segredo_e_rejeitado()
    {
        var jwt = CriarServico().GerarToken("maria@fcg.com", new[] { "Usuario" });

        var resultado = await new JsonWebTokenHandler().ValidateTokenAsync(jwt, new TokenValidationParameters
        {
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("outro-segredo-completamente-diferente-123")),
            ValidateIssuerSigningKey = true,
            ValidateIssuer = false,
            ValidateAudience = false
        });

        Assert.False(resultado.IsValid);
    }
}
