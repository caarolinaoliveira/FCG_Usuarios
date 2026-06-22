using FCG.Domain.Entities;

namespace FCG.Domain.Interfaces
{
    public interface IUsuarioRepository : IRepository<UsuarioEntity>
    {
        Task<UsuarioEntity?> ObterPorIdentityIdAsync(string identityUserId);
        Task<UsuarioEntity?> ObterPorEmailAsync(string email);
        Task AdicionarAsync(UsuarioEntity usuario);
    }
}