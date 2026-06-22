using FCG.Domain.Interfaces;
using FCG.Domain.Entities;
using FCG.Infrastructure.Context;
using FCG.Infrastructure.Repository;
using Microsoft.EntityFrameworkCore;

namespace FCG.Infrastructure.Repository
{
    public class UsuarioRepository : Repository<UsuarioEntity>, IUsuarioRepository
    {
        public UsuarioRepository(FcgDbContext context) : base(context)
        {
        }

        public async Task<UsuarioEntity?> ObterPorIdentityIdAsync(string identityUserId)
            => await DbSet.FirstOrDefaultAsync(u => u.IdentityUserId == identityUserId);

        public async Task<UsuarioEntity?> ObterPorEmailAsync(string email)
            => await DbSet.FirstOrDefaultAsync(u => u.Email == email);
    }
}