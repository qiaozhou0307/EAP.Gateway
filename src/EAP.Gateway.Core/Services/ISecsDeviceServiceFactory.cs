using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using EAP.Gateway.Core.Repositories;
using EAP.Gateway.Core.ValueObjects;

namespace EAP.Gateway.Core.Services;

/// <summary>
/// SECS设备服务工厂接口
/// </summary>
public interface ISecsDeviceServiceFactory
{
    ISecsDeviceService CreateDeviceService(EquipmentId equipmentId, DeviceConnectionConfig config);
    void ReleaseDeviceService(EquipmentId equipmentId);
}
