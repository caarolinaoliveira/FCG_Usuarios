using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace FCG.Application.Requests.Usuarios
{
    public sealed record AtualizarUsuarioRequest
    {
        [MaxLength(100, ErrorMessage = "Nome deve ter no máximo 100 caracteres")]
        public string? Nome { get; init; }

        [MaxLength(200, ErrorMessage = "Email deve ter no máximo 200 caracteres")]
        [EmailAddress(ErrorMessage = "Email inválido")]
        public string? Email { get; init; }

        public DateOnly? DataNascimento { get; init; }
    }
}