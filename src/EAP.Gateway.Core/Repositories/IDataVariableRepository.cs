using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using EAP.Gateway.Core.ValueObjects;

namespace EAP.Gateway.Core.Repositories;

/// <summary>
/// 数据变量仓储接口
/// </summary>
public interface IDataVariableRepository : IGenericRepository<DataVariable, DataVariableId>
{
    Task<IEnumerable<DataVariable>> GetByEquipmentIdAsync(EquipmentId equipmentId, CancellationToken cancellationToken = default);
    Task<DataVariable?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<IEnumerable<DataVariable>> GetActiveVariablesAsync(CancellationToken cancellationToken = default);
}
