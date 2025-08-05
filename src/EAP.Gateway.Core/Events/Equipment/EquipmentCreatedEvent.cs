using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using EAP.Gateway.Core.Events.Common;
using EAP.Gateway.Core.ValueObjects;

namespace EAP.Gateway.Core.Events.Equipment;

/// <summary>
/// 设备创建事件
/// </summary>
public sealed class EquipmentCreatedEvent : DomainEventBase
{
    public EquipmentId EquipmentId { get; }
    public string Name { get; }
    public IpEndpoint Endpoint { get; }
    public DateTime CreatedAt { get; }
    public string? CreatedBy { get; }

    public EquipmentCreatedEvent(EquipmentId equipmentId, string name, IpEndpoint endpoint, DateTime createdAt, string? createdBy = null)
    {
        EquipmentId = equipmentId;
        Name = name;
        Endpoint = endpoint;
        CreatedAt = createdAt;
        CreatedBy = createdBy;
    }
}
