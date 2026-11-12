using System.Text.Json;
using FCG.Application.Interfaces;
using FCG.Domain.Exceptions;
using FCG.Presentation.Middlewares;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace FCG.Presentation.Tests.Middlewares;

public class ExceptionMiddlewareTests
{
    private readonly IErroLogService _erroLog = Substitute.For<IErroLogService>();

    private static DefaultHttpContext NovoContexto()
    {
        var contexto = new DefaultHttpContext();
        contexto.Request.Path = "/api/usuarios/login";
        contexto.Request.Method = "POST";
        contexto.Response.Body = new MemoryStream();
        return contexto;
    }

    private static string LerMensagem(DefaultHttpContext contexto)
    {
        contexto.Response.Body.Position = 0;
        using var documento = JsonDocument.Parse(contexto.Response.Body);
        return documento.RootElement.GetProperty("error").GetString()!;
    }

    private async Task<DefaultHttpContext> Executar(Exception? excecao)
    {
        var contexto = NovoContexto();
        var middleware = new ExceptionMiddleware(
            _ => excecao is null ? Task.CompletedTask : throw excecao,
            NullLogger<ExceptionMiddleware>.Instance,
            _erroLog);

        await middleware.InvokeAsync(contexto);
        return contexto;
    }

    [Fact]
    public async Task Sem_excecao_a_requisicao_segue_sem_alteracao()
    {
        var contexto = await Executar(null);

        Assert.Equal(StatusCodes.Status200OK, contexto.Response.StatusCode);
        await _erroLog.DidNotReceiveWithAnyArgs().RegistrarAsync(default!, default!, default);
    }

    [Theory]
    [MemberData(nameof(ExcecoesEStatus))]
    public async Task Excecao_de_dominio_vira_o_status_correspondente_com_a_mensagem(Exception excecao, int statusEsperado)
    {
        var contexto = await Executar(excecao);

        Assert.Equal(statusEsperado, contexto.Response.StatusCode);
        Assert.Equal("application/json", contexto.Response.ContentType);
        Assert.Equal(excecao.Message, LerMensagem(contexto));
    }

    public static IEnumerable<object[]> ExcecoesEStatus()
    {
        yield return new object[] { new NotFoundException("Usuário não encontrado."), StatusCodes.Status404NotFound };
        yield return new object[] { new ConflictException("E-mail já cadastrado."), StatusCodes.Status409Conflict };
        yield return new object[] { new UnauthorizedException("Sem permissão."), StatusCodes.Status401Unauthorized };
    }

    [Fact]
    public async Task Excecao_inesperada_vira_500_sem_vazar_a_mensagem_interna()
    {
        var contexto = await Executar(new InvalidOperationException("connection string: Server=interno;Password=segredo"));

        Assert.Equal(StatusCodes.Status500InternalServerError, contexto.Response.StatusCode);
        Assert.Equal("Ocorreu um erro interno.", LerMensagem(contexto));
    }

    [Fact]
    public async Task Toda_excecao_e_registrada_no_log_de_erros_com_o_caminho_da_requisicao()
    {
        var excecao = new NotFoundException("Usuário não encontrado.");

        await Executar(excecao);

        await _erroLog.Received(1).RegistrarAsync(excecao, "/api/usuarios/login", Arg.Any<object?>());
    }
}
