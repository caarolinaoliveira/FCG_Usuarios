using FCG.Application.Interfaces;
using FCG.Application.Requests.Usuarios;
using FCG.Application.Responses.Usuarios;
using FCG.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FCG.Presentation.Controllers
{
    public class UsuarioController : ControllerBase
    {
        private const string CacheKeyRolesUsuarioPrefix = "usuarios:roles:";

        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITokenService _tokenService;
        private readonly IUsuarioService _usuarioService;
        private readonly IDistributedCache _cache;
        private readonly ILogger<UsuarioController> _logger;

        public UsuarioController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ITokenService tokenService,
            IUsuarioService usuarioService,
            IDistributedCache cache,
            ILogger<UsuarioController> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _tokenService = tokenService;
            _usuarioService = usuarioService;
            _cache = cache;
            _logger = logger;
        }

        [HttpPost("api/usuarios/registrar")]
        public async Task<IActionResult> Registrar([FromBody] CriarUsuarioRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var identityUser = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                EmailConfirmed = true
            };

            var identityResult = await _userManager.CreateAsync(identityUser, request.Senha);
            if (!identityResult.Succeeded)
            {
                _logger.LogWarning("Falha ao registrar usuário {Email}: {Erros}",
                    request.Email, string.Join("; ", identityResult.Errors.Select(e => e.Description)));
                return BadRequest(identityResult.Errors);
            }

            await _userManager.AddToRoleAsync(identityUser, "Usuario");

            await _usuarioService.CriarAsync(identityUser.Id, request);

            var roles = await _userManager.GetRolesAsync(identityUser);
            var token = _tokenService.GerarToken(identityUser.Email!, roles);

            _logger.LogInformation("Usuário {Id} registrado com sucesso ({Email})", identityUser.Id, identityUser.Email);

            return Ok(token);
        }

        [HttpPost("api/usuarios/login")]
        public async Task<IActionResult> Login([FromBody] LoginUsuarioRequest request)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Dados de entrada inválidos");
                return BadRequest(ModelState);
            }

            var response = await _signInManager.PasswordSignInAsync(
                request.Email, request.Senha, isPersistent: false, lockoutOnFailure: false);

            if (!response.Succeeded)
            {
                _logger.LogWarning("Tentativa de login falhou para o email {Email}", request.Email);
                return Unauthorized("Usuário ou senha inválidos.");
            }

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
                return Unauthorized();

            var roles = await ObterRolesComCacheAsync(user);
            var token = _tokenService.GerarToken(user.Email!, roles);

            _logger.LogInformation("Usuário {Email} autenticado com sucesso", user.Email);

            return Ok(token);
        }
                
        [HttpGet("api/usuarios/por-email")]
        public async Task<IActionResult> ObterPorEmail([FromQuery] string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                _logger.LogWarning("Requisição de busca por email recebida sem o parâmetro email");
                return BadRequest("O email é obrigatório.");
            }

            var usuario = await _usuarioService.ObterPorEmailAsync(email);
            if (usuario == null)
            {
                _logger.LogWarning("Usuário não encontrado para o email {Email}", email);
                return NotFound("Usuário não encontrado.");
            }

            _logger.LogInformation("Usuário {Id} encontrado para o email {Email}", usuario.Id, email);

            return Ok(new UsuarioIdResponse { Id = usuario.Id });
        }

        [Authorize]
        [HttpPost("api/usuarios/alterar-senha")]
        public async Task<IActionResult> AlterarSenha([FromBody] AlterarSenhaRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var usuario = await _userManager.FindByEmailAsync(request.Email);
            if (usuario == null)
            {
                _logger.LogWarning("Tentativa de alteração de senha para email inexistente: {Email}", request.Email);
                return NotFound("Usuário não encontrado.");
            }

            var result = await _userManager.ChangePasswordAsync(
                usuario,
                request.SenhaAtual,
                request.NovaSenha
            );

            if (!result.Succeeded)
            {
                _logger.LogWarning("Falha ao alterar senha do usuário {Email}: {Erros}",
                    request.Email, string.Join("; ", result.Errors.Select(e => e.Description)));
                return BadRequest(result.Errors);
            }

            _logger.LogInformation("Senha alterada com sucesso para o usuário {Email}", request.Email);

            return Ok("Senha alterada com sucesso.");
        }

        private async Task<IList<string>> ObterRolesComCacheAsync(ApplicationUser user)
        {
            var cacheKey = $"{CacheKeyRolesUsuarioPrefix}{user.Id}";
            // O cache é só otimização: se o Redis falhar, o login segue pelo Identity.
            try
            {
                var cacheado = await _cache.GetStringAsync(cacheKey);

                if (cacheado is not null)
                {
                    NewRelic.Api.Agent.NewRelic.GetAgent().CurrentTransaction.AddCustomAttribute("cacheHit", true);
                    return JsonSerializer.Deserialize<List<string>>(cacheado) ?? new List<string>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao ler o cache de roles do usuário {UserId}; consultando o Identity", user.Id);
            }

            NewRelic.Api.Agent.NewRelic.GetAgent().CurrentTransaction.AddCustomAttribute("cacheHit", false);

            var roles = await _userManager.GetRolesAsync(user);

            try
            {
                await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(roles),
                    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao gravar o cache de roles do usuário {UserId}", user.Id);
            }

            return roles;
        }
    }
}