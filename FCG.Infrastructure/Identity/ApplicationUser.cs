
using Microsoft.AspNetCore.Identity;

namespace FCG.Infrastructure.Identity
{
    public class ApplicationUser : IdentityUser
    {
        public Guid UsuarioId { get; set; } 
    }
}
