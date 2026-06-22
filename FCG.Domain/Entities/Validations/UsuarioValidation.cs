using FluentValidation;

namespace FCG.Domain.Entities.Validations
{
    public class UsuarioValidation : AbstractValidator<UsuarioEntity>
    {
        public UsuarioValidation()
        {
            RuleFor(u => u.Nome)
                .NotEmpty().WithMessage("O nome é obrigatório.")
                .Length(2, 100).WithMessage("O nome deve conter entre 2 e 100 caracteres.");

            RuleFor(u => u.Email)
                .NotEmpty().WithMessage("O email é obrigatório.")
                .EmailAddress().WithMessage("O email deve ser válido.");

            RuleFor(u => u.DataNascimento)
                .NotEmpty().WithMessage("A data de nascimento é obrigatória.")
                .LessThan(DateOnly.FromDateTime(DateTime.UtcNow))
                .WithMessage("A data de nascimento deve ser no passado.");

            RuleFor(u => u.IdentityUserId)
                .NotEmpty().WithMessage("O vínculo com o usuário de autenticação é obrigatório.");
        }
    }
}