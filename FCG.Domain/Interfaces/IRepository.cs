using System.Linq.Expressions;
using FCG.Domain.Entities;

namespace FCG.Domain.Interfaces
{
    public interface IRepository<TEntity> : IDisposable where TEntity : Entity
    {
        Task AdicionarAsync(TEntity entity);
        Task<TEntity?> ObterPorIdAsync(Guid id);
        Task<List<TEntity>> ObterTodosAsync();
        Task AtualizarAsync(TEntity entity);
        Task RemoverAsync(Guid id);
        Task<int> SaveChangesAsync();
        Task<IEnumerable<TEntity>> ObterPorFiltroAsync(Expression<Func<TEntity, bool>> filtro);
    }
}