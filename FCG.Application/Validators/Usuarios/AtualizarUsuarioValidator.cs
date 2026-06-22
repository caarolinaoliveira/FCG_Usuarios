using FCG.Application.Requests.Usuarios;
using FluentValidation;

namespace FCG.Application.Validators.Usuarios
{
    public class AtualizarUsuarioValidator : AbstractValidator<AtualizarUsuarioRequest>
    {
        public AtualizarUsuarioValidator()
        {
            RuleFor(x => x.Nome)
                .NotEmpty().WithMessage("Nome é obrigatório.")
                .MinimumLength(3).WithMessage("Nome deve ter ao menos 3 caracteres.")
                .MaximumLength(100).WithMessage("Nome deve ter no máximo 100 caracteres.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("E-mail é obrigatório.")
                .EmailAddress().WithMessage("Formato de e-mail inválido.");

            RuleFor(x => x.DataNascimento)
                .NotEmpty().WithMessage("Data de nascimento é obrigatória.")
                .Must(data => DateTime.UtcNow.Year - data.Value.Year >= 18)
                .WithMessage("Usuário deve ter ao menos 18 anos.");
        }
    }
}