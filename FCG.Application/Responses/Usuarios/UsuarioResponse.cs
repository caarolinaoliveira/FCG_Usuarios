namespace FCG.Application.Responses.Usuarios
{
    public sealed record UsuarioResponse
    {
        public Guid Id { get; init; }
        public string Nome { get; init; }
        public string Email { get; init; }
        public DateTime DataNascimento { get; init; }
        public DateTime DataCriacao { get; init; }
    }
}