using FCG.Domain.Entities;
using FCG.Domain.Enums;

namespace FCG.Domain.Tests.Entities;

public class UsuarioEntityTests
{
    [Fact]
    public void Novo_usuario_recebe_id_proprio()
    {
        var primeiro = new UsuarioEntity();
        var segundo = new UsuarioEntity();

        Assert.NotEqual(Guid.Empty, primeiro.Id);
        Assert.NotEqual(primeiro.Id, segundo.Id);
    }

    [Fact]
    public void Novo_usuario_comeca_ativo()
    {
        Assert.Equal(UsuarioStatusEnum.Ativo, new UsuarioEntity().StatusConta);
    }
}
