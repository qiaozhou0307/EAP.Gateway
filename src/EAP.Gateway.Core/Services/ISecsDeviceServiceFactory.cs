using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using EAP.Gateway.Core.Repositories;
using EAP.Gateway.Core.ValueObjects;

namespace EAP.Gateway.Core.Services;

public interface ISecsDeviceServiceFactory
{
    ISecsDeviceService CreateDeviceService(EquipmentId equipmentId, EquipmentConfiguration configuration);
}
