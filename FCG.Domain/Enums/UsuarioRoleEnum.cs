using System.ComponentModel;

namespace FCG.Domain.Enums
{
    public enum UsuarioRoleEnum
    {
        [Description("Usuário Comum")]
        Usuario = 0,
        
        [Description("Administrador")]
        Administrador = 1
    }
}