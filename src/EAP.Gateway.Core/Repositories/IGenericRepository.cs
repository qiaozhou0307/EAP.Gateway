using EAP.Gateway.Core.Common;

namespace EAP.Gateway.Core.Repositories;

/// <summary>
/// 通用仓储接口
/// </summary>
public interface IGenericRepository<TEntity, TId>
    where TEntity : Entity<TId>
    where TId : ValueObject
{
    Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);
    Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<TEntity>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(TId id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(TId id, CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
}
