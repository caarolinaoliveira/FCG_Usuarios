using System.Text;
using FCG.Application.Interfaces;
using FCG.Application.Requests.Usuarios;
using FCG.Application.Responses.Usuarios;
using FCG.Domain.Entities;
using FCG.Infrastructure.Identity;
using FCG.Presentation.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace FCG.Presentation.Tests.Controllers;

public class UsuarioControllerTests
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly IUsuarioService _usuarioService = Substitute.For<IUsuarioService>();
    private readonly IDistributedCache _cache = Substitute.For<IDistributedCache>();
    private readonly UsuarioController _controller;

    public UsuarioControllerTests()
    {
        _userManager = Substitute.For<UserManager<ApplicationUser>>(
            Substitute.For<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null);
        _signInManager = Substitute.For<SignInManager<ApplicationUser>>(
            _userManager, Substitute.For<IHttpContextAccessor>(),
            Substitute.For<IUserClaimsPrincipalFactory<ApplicationUser>>(), null, null, null, null);

        _tokenService.GerarToken(Arg.Any<string>(), Arg.Any<IEnumerable<string>>()).Returns("jwt-token");
        CacheSemRoles();

        _controller = new UsuarioController(
            _signInManager, _userManager, _tokenService, _usuarioService, _cache, NullLogger<UsuarioController>.Instance);
    }

    private static ApplicationUser UsuarioIdentity() => new() { Id = "identity-123", Email = "maria@fcg.com", UserName = "maria@fcg.com" };

    private static CriarUsuarioRequest RegistroValido() => new()
    {
        Nome = "Maria Silva",
        Email = "maria@fcg.com",
        Senha = "Senha@123",
        ConfirmacaoSenha = "Senha@123",
        DataNascimento = new DateOnly(1990, 5, 10)
    };

    private static LoginUsuarioRequest LoginValido() => new() { Email = "maria@fcg.com", Senha = "Senha@123" };

    private void CacheSemRoles()
        => _cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<byte[]?>(null));

    private void CacheComRoles(string json)
        => _cache.GetAsync("usuarios:roles:identity-123", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<byte[]?>(Encoding.UTF8.GetBytes(json)));

    private void LoginAceitoPara(ApplicationUser usuario, params string[] roles)
    {
        _signInManager.PasswordSignInAsync(usuario.Email!, Arg.Any<string>(), false, false).Returns(SignInResult.Success);
        _userManager.FindByEmailAsync(usuario.Email!).Returns(usuario);
        _userManager.GetRolesAsync(usuario).Returns((IList<string>)roles.ToList());
    }

    public class Registrar : UsuarioControllerTests
    {
        [Fact]
        public async Task Modelo_invalido_devolve_400_sem_criar_nada()
        {
            _controller.ModelState.AddModelError("Email", "obrigatório");

            var resultado = await _controller.Registrar(RegistroValido());

            Assert.IsType<BadRequestObjectResult>(resultado);
            await _userManager.DidNotReceiveWithAnyArgs().CreateAsync(default!, default!);
            await _usuarioService.DidNotReceiveWithAnyArgs().CriarAsync(default!, default!);
        }

        [Fact]
        public async Task Sucesso_cria_identity_atribui_role_persiste_e_devolve_token()
        {
            ApplicationUser? criado = null;
            _userManager.CreateAsync(Arg.Do<ApplicationUser>(u => criado = u), "Senha@123").Returns(IdentityResult.Success);
            _userManager.AddToRoleAsync(Arg.Any<ApplicationUser>(), "Usuario").Returns(IdentityResult.Success);
            _userManager.GetRolesAsync(Arg.Any<ApplicationUser>()).Returns((IList<string>)new List<string> { "Usuario" });
            var request = RegistroValido();

            var resultado = await _controller.Registrar(request);

            var ok = Assert.IsType<OkObjectResult>(resultado);
            Assert.Equal("jwt-token", ok.Value);
            Assert.Equal("maria@fcg.com", criado!.Email);
            Assert.Equal("maria@fcg.com", criado.UserName);
            await _userManager.Received(1).AddToRoleAsync(criado, "Usuario");
            await _usuarioService.Received(1).CriarAsync(criado.Id, request);
            _tokenService.Received(1).GerarToken("maria@fcg.com", Arg.Is<IEnumerable<string>>(r => r.Single() == "Usuario"));
        }

        [Theory]
        [InlineData("DuplicateEmail")]
        [InlineData("DuplicateUserName")]
        public async Task Email_duplicado_devolve_400_e_nao_persiste_nem_gera_token(string codigoDoErro)
        {
            _userManager.CreateAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>())
                .Returns(IdentityResult.Failed(new IdentityError { Code = codigoDoErro, Description = "E-mail já em uso." }));

            var resultado = await _controller.Registrar(RegistroValido());

            Assert.IsType<BadRequestObjectResult>(resultado);
            await _userManager.DidNotReceiveWithAnyArgs().AddToRoleAsync(default!, default!);
            await _usuarioService.DidNotReceiveWithAnyArgs().CriarAsync(default!, default!);
            _tokenService.DidNotReceiveWithAnyArgs().GerarToken(default!, default!);
        }

        [Fact]
        public async Task Senha_recusada_pelo_identity_devolve_400()
        {
            _userManager.CreateAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>())
                .Returns(IdentityResult.Failed(new IdentityError { Code = "PasswordTooShort", Description = "Senha curta." }));

            var resultado = await _controller.Registrar(RegistroValido());

            Assert.IsType<BadRequestObjectResult>(resultado);
        }
    }

    public class Login : UsuarioControllerTests
    {
        [Fact]
        public async Task Modelo_invalido_devolve_400_sem_tentar_autenticar()
        {
            _controller.ModelState.AddModelError("Email", "obrigatório");

            var resultado = await _controller.Login(LoginValido());

            Assert.IsType<BadRequestObjectResult>(resultado);
            await _signInManager.DidNotReceiveWithAnyArgs().PasswordSignInAsync(default(string)!, default!, default, default);
        }

        [Fact]
        public async Task Sucesso_devolve_token_com_as_roles_do_usuario()
        {
            var usuario = UsuarioIdentity();
            LoginAceitoPara(usuario, "Admin");
            IEnumerable<string>? rolesNoToken = null;
            _tokenService.GerarToken("maria@fcg.com", Arg.Do<IEnumerable<string>>(r => rolesNoToken = r.ToList())).Returns("jwt-token");

            var resultado = await _controller.Login(LoginValido());

            var ok = Assert.IsType<OkObjectResult>(resultado);
            Assert.Equal("jwt-token", ok.Value);
            Assert.Equal(new[] { "Admin" }, rolesNoToken);
        }

        [Fact]
        public async Task Senha_errada_devolve_401_sem_gerar_token()
        {
            _signInManager.PasswordSignInAsync("maria@fcg.com", "errada", false, false).Returns(SignInResult.Failed);

            var resultado = await _controller.Login(LoginValido() with { Senha = "errada" });

            var naoAutorizado = Assert.IsType<UnauthorizedObjectResult>(resultado);
            Assert.Equal("Usuário ou senha inválidos.", naoAutorizado.Value);
            _tokenService.DidNotReceiveWithAnyArgs().GerarToken(default!, default!);
        }

        [Fact]
        public async Task Usuario_inexistente_recebe_a_mesma_resposta_da_senha_errada()
        {
            _signInManager.PasswordSignInAsync("ninguem@fcg.com", Arg.Any<string>(), false, false).Returns(SignInResult.Failed);

            var resultado = await _controller.Login(LoginValido() with { Email = "ninguem@fcg.com" });

            var naoAutorizado = Assert.IsType<UnauthorizedObjectResult>(resultado);
            Assert.Equal("Usuário ou senha inválidos.", naoAutorizado.Value);
        }

        [Fact]
        public async Task Autenticou_mas_usuario_sumiu_devolve_401()
        {
            _signInManager.PasswordSignInAsync("maria@fcg.com", Arg.Any<string>(), false, false).Returns(SignInResult.Success);
            _userManager.FindByEmailAsync("maria@fcg.com").Returns((ApplicationUser?)null);

            var resultado = await _controller.Login(LoginValido());

            Assert.IsType<UnauthorizedResult>(resultado);
        }

        [Fact]
        public async Task Falha_de_login_nao_consulta_o_cache()
        {
            _signInManager.PasswordSignInAsync(Arg.Any<string>(), Arg.Any<string>(), false, false).Returns(SignInResult.Failed);

            await _controller.Login(LoginValido());

            await _cache.DidNotReceiveWithAnyArgs().GetAsync(default!, default);
        }
    }

    public class CacheDeRoles : UsuarioControllerTests
    {
        [Fact]
        public async Task Miss_busca_as_roles_no_identity_e_grava_no_cache_por_5_minutos()
        {
            var usuario = UsuarioIdentity();
            LoginAceitoPara(usuario, "Usuario", "Admin");

            await _controller.Login(LoginValido());

            await _userManager.Received(1).GetRolesAsync(usuario);
            await _cache.Received(1).SetAsync(
                "usuarios:roles:identity-123",
                Arg.Is<byte[]>(b => Encoding.UTF8.GetString(b) == "[\"Usuario\",\"Admin\"]"),
                Arg.Is<DistributedCacheEntryOptions>(o => o.AbsoluteExpirationRelativeToNow == TimeSpan.FromMinutes(5)),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Hit_usa_as_roles_do_cache_sem_consultar_o_identity_nem_regravar()
        {
            var usuario = UsuarioIdentity();
            LoginAceitoPara(usuario, "Usuario");
            CacheComRoles("[\"Admin\"]");
            IEnumerable<string>? rolesNoToken = null;
            _tokenService.GerarToken("maria@fcg.com", Arg.Do<IEnumerable<string>>(r => rolesNoToken = r.ToList())).Returns("jwt-token");

            var resultado = await _controller.Login(LoginValido());

            Assert.IsType<OkObjectResult>(resultado);
            Assert.Equal(new[] { "Admin" }, rolesNoToken);
            await _userManager.DidNotReceiveWithAnyArgs().GetRolesAsync(default!);
            await _cache.DidNotReceiveWithAnyArgs().SetAsync(default!, default!, default!, default);
        }

        [Fact]
        public async Task Redis_fora_do_ar_nao_impede_o_login()
        {
            var usuario = UsuarioIdentity();
            LoginAceitoPara(usuario, "Usuario");
            _cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).ThrowsAsync(new InvalidOperationException("Redis indisponível"));
            _cache.SetAsync(default!, default!, default!, default).ThrowsAsyncForAnyArgs(new InvalidOperationException("Redis indisponível"));
            IEnumerable<string>? rolesNoToken = null;
            _tokenService.GerarToken("maria@fcg.com", Arg.Do<IEnumerable<string>>(r => rolesNoToken = r.ToList())).Returns("jwt-token");

            var resultado = await _controller.Login(LoginValido());

            Assert.IsType<OkObjectResult>(resultado);
            Assert.Equal(new[] { "Usuario" }, rolesNoToken);
            await _userManager.Received(1).GetRolesAsync(usuario);
        }

        [Fact]
        public async Task Falha_ao_gravar_no_cache_nao_impede_o_login()
        {
            var usuario = UsuarioIdentity();
            LoginAceitoPara(usuario, "Usuario");
            _cache.SetAsync(default!, default!, default!, default).ThrowsAsyncForAnyArgs(new InvalidOperationException("Redis indisponível"));

            var resultado = await _controller.Login(LoginValido());

            Assert.IsType<OkObjectResult>(resultado);
        }

        [Fact]
        public async Task Entrada_corrompida_no_cache_e_tratada_como_miss()
        {
            var usuario = UsuarioIdentity();
            LoginAceitoPara(usuario, "Admin");
            CacheComRoles("isto-nao-e-json");

            var resultado = await _controller.Login(LoginValido());

            Assert.IsType<OkObjectResult>(resultado);
            await _userManager.Received(1).GetRolesAsync(usuario);
        }

        [Fact]
        public async Task Chave_do_cache_e_por_usuario()
        {
            var usuario = UsuarioIdentity();
            LoginAceitoPara(usuario, "Usuario");

            await _controller.Login(LoginValido());

            await _cache.Received(1).GetAsync("usuarios:roles:identity-123", Arg.Any<CancellationToken>());
        }
    }

    public class AlterarSenha : UsuarioControllerTests
    {
        private static AlterarSenhaRequest Request() => new()
        {
            Email = "maria@fcg.com",
            SenhaAtual = "Senha@123",
            NovaSenha = "NovaSenha@456",
            ConfirmarNovaSenha = "NovaSenha@456"
        };

        [Fact]
        public async Task Modelo_invalido_devolve_400()
        {
            _controller.ModelState.AddModelError("NovaSenha", "curta");

            Assert.IsType<BadRequestObjectResult>(await _controller.AlterarSenha(Request()));
        }

        [Fact]
        public async Task Usuario_inexistente_devolve_404_sem_tentar_trocar_a_senha()
        {
            _userManager.FindByEmailAsync("maria@fcg.com").Returns((ApplicationUser?)null);

            Assert.IsType<NotFoundObjectResult>(await _controller.AlterarSenha(Request()));
            await _userManager.DidNotReceiveWithAnyArgs().ChangePasswordAsync(default!, default!, default!);
        }

        [Fact]
        public async Task Senha_atual_errada_devolve_400()
        {
            var usuario = UsuarioIdentity();
            _userManager.FindByEmailAsync("maria@fcg.com").Returns(usuario);
            _userManager.ChangePasswordAsync(usuario, "Senha@123", "NovaSenha@456")
                .Returns(IdentityResult.Failed(new IdentityError { Code = "PasswordMismatch", Description = "Senha incorreta." }));

            Assert.IsType<BadRequestObjectResult>(await _controller.AlterarSenha(Request()));
        }

        [Fact]
        public async Task Sucesso_troca_a_senha_e_devolve_200()
        {
            var usuario = UsuarioIdentity();
            _userManager.FindByEmailAsync("maria@fcg.com").Returns(usuario);
            _userManager.ChangePasswordAsync(usuario, "Senha@123", "NovaSenha@456").Returns(IdentityResult.Success);

            var resultado = await _controller.AlterarSenha(Request());

            Assert.Equal("Senha alterada com sucesso.", Assert.IsType<OkObjectResult>(resultado).Value);
        }
    }

    public class ObterPorEmail : UsuarioControllerTests
    {
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Email_em_branco_devolve_400(string email)
        {
            Assert.IsType<BadRequestObjectResult>(await _controller.ObterPorEmail(email));
        }

        [Fact]
        public async Task Usuario_inexistente_devolve_404()
        {
            _usuarioService.ObterPorEmailAsync("ninguem@fcg.com").Returns((UsuarioEntity?)null);

            Assert.IsType<NotFoundObjectResult>(await _controller.ObterPorEmail("ninguem@fcg.com"));
        }

        [Fact]
        public async Task Usuario_existente_devolve_o_id()
        {
            var usuario = new UsuarioEntity { Email = "maria@fcg.com" };
            _usuarioService.ObterPorEmailAsync("maria@fcg.com").Returns(usuario);

            var resultado = await _controller.ObterPorEmail("maria@fcg.com");

            var corpo = Assert.IsType<UsuarioIdResponse>(Assert.IsType<OkObjectResult>(resultado).Value);
            Assert.Equal(usuario.Id, corpo.Id);
        }
    }
}
