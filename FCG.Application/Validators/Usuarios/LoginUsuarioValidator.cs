using FCG.Application.Requests.Usuarios;
using FluentValidation;

namespace FCG.Application.Validators.Usuarios
{
    public class LoginUsuarioValidator : AbstractValidator<LoginUsuarioRequest>
    {
        public LoginUsuarioValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("O email é obrigatório.")
                .EmailAddress().WithMessage("O email deve ser válido.");

            RuleFor(x => x.Senha)
                .NotEmpty().WithMessage("A senha é obrigatória.");
        }
    }
}