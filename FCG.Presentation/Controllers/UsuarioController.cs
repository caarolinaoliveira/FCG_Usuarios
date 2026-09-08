using FCG.Application.Interfaces;
using FCG.Application.Requests.Usuarios;
using FCG.Application.Responses.Usuarios;
using FCG.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace FCG.Presentation.Controllers
{
    public class UsuarioController : ControllerBase
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITokenService _tokenService;
        private readonly IUsuarioService _usuarioService;

        public UsuarioController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ITokenService tokenService,
            IUsuarioService usuarioService)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _tokenService = tokenService;
            _usuarioService = usuarioService;
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
                return BadRequest(identityResult.Errors);

            await _userManager.AddToRoleAsync(identityUser, "Usuario");

            await _usuarioService.CriarAsync(identityUser.Id, request);

            var roles = await _userManager.GetRolesAsync(identityUser);
            var token = _tokenService.GerarToken(identityUser.Email!, roles);

            return Ok(token);
        }

        [HttpPost("api/usuarios/login")]
        public async Task<IActionResult> Login([FromBody] LoginUsuarioRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var response = await _signInManager.PasswordSignInAsync(
                request.Email,
                request.Senha,
                isPersistent: false,
                lockoutOnFailure: false
            );

            if (!response.Succeeded)
                return Unauthorized("Usuário ou senha inválidos.");

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
                return Unauthorized();

            var roles = await _userManager.GetRolesAsync(user);
            var token = _tokenService.GerarToken(user.Email!, roles);

            return Ok(token);
        }
        
        [HttpGet("api/usuarios/por-email")]
        public async Task<IActionResult> ObterPorEmail([FromQuery] string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return BadRequest("O email é obrigatório.");

            var usuario = await _usuarioService.ObterPorEmailAsync(email);
            if (usuario == null)
                return NotFound("Usuário não encontrado.");

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
                return NotFound("Usuário não encontrado.");

            var result = await _userManager.ChangePasswordAsync(
                usuario,
                request.SenhaAtual,
                request.NovaSenha
            );

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok("Senha alterada com sucesso.");
        }
    }
}