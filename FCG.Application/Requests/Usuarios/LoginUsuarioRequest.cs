using System.ComponentModel.DataAnnotations;

namespace  FCG.Application.Requests.Usuarios
{
    public sealed record LoginUsuarioRequest
    {
        [Required(ErrorMessage = "O email é obrigatório.")]
        [EmailAddress(ErrorMessage = "O email deve ser válido.")]
        public string Email { get; init; }

        [Required(ErrorMessage = "A senha é obrigatória.")]
        public string Senha { get; init; }
    }
    
}