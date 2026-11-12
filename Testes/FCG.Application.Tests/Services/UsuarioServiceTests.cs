using FCG.Application.Events;
using FCG.Application.Interfaces;
using FCG.Application.Requests.Usuarios;
using FCG.Application.Services;
using FCG.Domain.Entities;
using FCG.Domain.Interfaces;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace FCG.Application.Tests.Services;

public class UsuarioServiceTests
{
    private readonly IUsuarioRepository _repository = Substitute.For<IUsuarioRepository>();
    private readonly IMessagePublisher _publisher = Substitute.For<IMessagePublisher>();
    private readonly UsuarioService _service;

    public UsuarioServiceTests()
    {
        _service = new UsuarioService(_repository, _publisher);
    }

    private static CriarUsuarioRequest RequestValido() => new()
    {
        Nome = "Maria Silva",
        Email = "maria@fcg.com",
        Senha = "Senha@123",
        ConfirmacaoSenha = "Senha@123",
        DataNascimento = new DateOnly(1990, 5, 10)
    };

    [Fact]
    public async Task CriarAsync_persiste_o_usuario_com_os_dados_do_request()
    {
        UsuarioEntity? salvo = null;
        await _repository.AdicionarAsync(Arg.Do<UsuarioEntity>(u => salvo = u));

        await _service.CriarAsync("identity-123", RequestValido());

        Assert.NotNull(salvo);
        Assert.Equal("Maria Silva", salvo!.Nome);
        Assert.Equal("maria@fcg.com", salvo.Email);
        Assert.Equal(new DateOnly(1990, 5, 10), salvo.DataNascimento);
        Assert.Equal("identity-123", salvo.IdentityUserId);
    }

    [Fact]
    public async Task CriarAsync_publica_user_created_com_os_dados_do_usuario_salvo()
    {
        UsuarioEntity? salvo = null;
        await _repository.AdicionarAsync(Arg.Do<UsuarioEntity>(u => salvo = u));
        UserCreatedEvent? evento = null;
        await _publisher.PublicarAsync(Arg.Do<UserCreatedEvent>(e => evento = e), "user.created");

        await _service.CriarAsync("identity-123", RequestValido());

        await _publisher.Received(1).PublicarAsync(Arg.Any<UserCreatedEvent>(), "user.created");
        Assert.NotNull(evento);
        Assert.Equal(salvo!.Id, evento!.UserId);
        Assert.Equal("Maria Silva", evento.Nome);
        Assert.Equal("maria@fcg.com", evento.Email);
        Assert.True(evento.CriadoEm > DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task CriarAsync_grava_no_banco_antes_de_publicar()
    {
        await _service.CriarAsync("identity-123", RequestValido());

        Received.InOrder(() =>
        {
            _repository.AdicionarAsync(Arg.Any<UsuarioEntity>());
            _publisher.PublicarAsync(Arg.Any<UserCreatedEvent>(), "user.created");
        });
    }

    [Fact]
    public async Task CriarAsync_nao_publica_quando_o_banco_falha()
    {
        _repository.AdicionarAsync(Arg.Any<UsuarioEntity>()).ThrowsAsync(new InvalidOperationException("banco fora do ar"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CriarAsync("identity-123", RequestValido()));

        await _publisher.DidNotReceiveWithAnyArgs().PublicarAsync<UserCreatedEvent>(default!, default!);
    }

    [Fact]
    public async Task ObterPorEmailAsync_devolve_o_usuario_do_repositorio()
    {
        var usuario = new UsuarioEntity { Email = "maria@fcg.com" };
        _repository.ObterPorEmailAsync("maria@fcg.com").Returns(usuario);

        var resultado = await _service.ObterPorEmailAsync("maria@fcg.com");

        Assert.Same(usuario, resultado);
    }

    [Fact]
    public async Task ObterPorEmailAsync_devolve_nulo_quando_nao_existe()
    {
        _repository.ObterPorEmailAsync("ninguem@fcg.com").Returns((UsuarioEntity?)null);

        Assert.Null(await _service.ObterPorEmailAsync("ninguem@fcg.com"));
    }

    [Fact]
    public async Task ObterPorIdentityIdAsync_devolve_o_usuario_do_repositorio()
    {
        var usuario = new UsuarioEntity { IdentityUserId = "identity-123" };
        _repository.ObterPorIdentityIdAsync("identity-123").Returns(usuario);

        Assert.Same(usuario, await _service.ObterPorIdentityIdAsync("identity-123"));
    }
}
