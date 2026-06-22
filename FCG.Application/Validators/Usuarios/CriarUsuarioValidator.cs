using FCG.Application.Requests.Usuarios;
using FluentValidation;
using System;


namespace FCG.Application.Validators.Usuarios
{
    public class CriarUsuarioValidator : AbstractValidator<CriarUsuarioRequest>
    {
        public CriarUsuarioValidator()
        {
            RuleFor(x => x.Nome)
                .NotEmpty().WithMessage("Nome é obrigatório.")
                .MinimumLength(3).WithMessage("Nome deve ter ao menos 3 caracteres.")
                .MaximumLength(100).WithMessage("Nome deve ter no máximo 100 caracteres.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("E-mail é obrigatório.")
                .EmailAddress().WithMessage("Formato de e-mail inválido.");

            RuleFor(x => x.Senha)
                .NotEmpty().WithMessage("Senha é obrigatória.")
                .MinimumLength(8).WithMessage("Mínimo de 8 caracteres.")
                .Matches("[A-Z]").WithMessage("Deve conter ao menos uma letra maiúscula.")
                .Matches("[a-z]").WithMessage("Deve conter ao menos uma letra minúscula.")
                .Matches("[0-9]").WithMessage("Deve conter ao menos um número.")
                .Matches("[^a-zA-Z0-9]").WithMessage("Deve conter ao menos um caractere especial.");

            RuleFor(x => x.DataNascimento)
                .NotEmpty().WithMessage("Data de nascimento é obrigatória.")
                .Must(data => data <= DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-18)))

                .WithMessage("Usuário deve ter ao menos 18 anos.");
        }
    }
}