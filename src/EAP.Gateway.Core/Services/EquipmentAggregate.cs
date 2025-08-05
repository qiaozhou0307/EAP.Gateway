using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using EAP.Gateway.Core.Repositories;

namespace EAP.Gateway.Core.Services;

/// <summary>
/// SECS设备管理器接口
/// </summary>
public interface ISecsDeviceManager
{
    Task<ISecsDeviceService?> GetDeviceServiceAsync(EquipmentId equipmentId, CancellationToken cancellationToken = default);
    Task<bool> RegisterDeviceServiceAsync(EquipmentId equipmentId, ISecsDeviceService deviceService, CancellationToken cancellationToken = default);
    Task<bool> UnregisterDeviceServiceAsync(EquipmentId equipmentId, CancellationToken cancellationToken = default);
    Task<IEnumerable<EquipmentId>> GetRegisteredDevicesAsync(CancellationToken cancellationToken = default);
    Task<int> GetDeviceCountAsync(CancellationToken cancellationToken = default);
}
