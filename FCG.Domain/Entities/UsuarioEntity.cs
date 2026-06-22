using FCG.Domain.Enums;

namespace FCG.Domain.Entities
{
    public class UsuarioEntity : Entity
    {
        public string Nome { get; set; }
        public string Email { get; set; }
        public string IdentityUserId { get; set; }
        public DateOnly DataNascimento { get; set; }
        public UsuarioStatusEnum StatusConta { get; set; } = UsuarioStatusEnum.Ativo;
        public DateTime DataCriacao { get; set; }
    }
}