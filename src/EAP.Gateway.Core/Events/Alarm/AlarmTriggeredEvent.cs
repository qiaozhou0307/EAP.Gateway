using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using EAP.Gateway.Core.Entities;
using EAP.Gateway.Core.Events.Common;

namespace EAP.Gateway.Core.Events.Alarm;

/// <summary>
/// 报警触发事件
/// </summary>
public sealed class AlarmTriggeredEvent : DomainEventBase
{
    public EquipmentId EquipmentId { get; }
    public AlarmEvent Alarm { get; }
    public DateTime TriggeredAt { get; }

    public AlarmTriggeredEvent(EquipmentId equipmentId, AlarmEvent alarm, DateTime triggeredAt)
    {
        EquipmentId = equipmentId;
        Alarm = alarm;
        TriggeredAt = triggeredAt;
    }
}
