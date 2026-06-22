using System.ComponentModel.DataAnnotations;

public sealed record AlterarSenhaRequest
{
    [Required(ErrorMessage = "E-mail é obrigatório")]
    [EmailAddress(ErrorMessage = "E-mail inválido")]
    public string Email { get; init; }

    [Required(ErrorMessage = "Senha atual é obrigatória")]
    public string SenhaAtual { get; init; }

    [Required(ErrorMessage = "Nova senha é obrigatória")]
    [MinLength(8, ErrorMessage = "Nova senha deve ter no mínimo 8 caracteres")]
    public string NovaSenha { get; init; }

    [Required(ErrorMessage = "Confirmação de senha é obrigatória")]
    [Compare("NovaSenha", ErrorMessage = "As senhas não coincidem")]
    public string ConfirmarNovaSenha { get; init; }
}