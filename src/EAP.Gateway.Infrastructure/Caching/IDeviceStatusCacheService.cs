using EAP.Gateway.Core.Aggregates.EquipmentAggregate;

namespace EAP.Gateway.Infrastructure.Caching;

/// <summary>
/// 设备状态缓存服务接口
/// </summary>
public interface IDeviceStatusCacheService
{
    Task<DeviceStatus?> GetDeviceStatusAsync(EquipmentId equipmentId, CancellationToken cancellationToken = default);
    Task SetDeviceStatusAsync(EquipmentId equipmentId, DeviceStatus status, CancellationToken cancellationToken = default);
    Task RemoveDeviceStatusAsync(EquipmentId equipmentId, CancellationToken cancellationToken = default);
    Task<IEnumerable<DeviceStatus>> GetAllDeviceStatusesAsync(CancellationToken cancellationToken = default);
    Task ClearAllAsync(CancellationToken cancellationToken = default);
}
