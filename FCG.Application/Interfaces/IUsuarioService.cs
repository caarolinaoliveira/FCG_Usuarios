using FCG.Application.Requests.Usuarios;
using FCG.Application.Responses.Usuarios;
using FCG.Domain.Entities;

namespace FCG.Application.Interfaces
{
    public interface IUsuarioService
    {
        Task CriarAsync(string identityUserId, CriarUsuarioRequest request);
        Task<UsuarioEntity?> ObterPorIdentityIdAsync(string identityUserId);

    }
}