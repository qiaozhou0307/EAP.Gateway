using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using EAP.Gateway.Core.Models;
using EAP.Gateway.Core.ValueObjects;

namespace EAP.Gateway.Core.Repositories;

/// <summary>
/// 设备状态缓存服务接口（领域服务）
/// 使用领域模型而不是DTO，保持Core层的纯净性
/// </summary>
public interface IDeviceStatusCacheService
{
    /// <summary>
    /// 获取设备状态
    /// </summary>
    Task<EquipmentStatus?> GetEquipmentStatusAsync(EquipmentId equipmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 设置设备状态
    /// </summary>
    Task<bool> SetEquipmentStatusAsync(EquipmentStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取数据变量
    /// </summary>
    Task<DataVariables?> GetDataVariablesAsync(EquipmentId equipmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 设置数据变量
    /// </summary>
    Task<bool> SetDataVariablesAsync(DataVariables dataVariables, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新单个数据变量
    /// </summary>
    Task<bool> UpdateDataVariableAsync(EquipmentId equipmentId, uint variableId, object value, string? name = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量更新数据变量
    /// </summary>
    Task<bool> UpdateDataVariablesAsync(EquipmentId equipmentId, IReadOnlyDictionary<uint, object> updates, CancellationToken cancellationToken = default);

    /// <summary>
    /// 移除设备状态
    /// </summary>
    Task<bool> RemoveEquipmentStatusAsync(EquipmentId equipmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 新增方法 - 获取所有设备状态:
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<IEnumerable<EquipmentStatus>> GetAllEquipmentStatusAsync(CancellationToken cancellationToken = default);

}
