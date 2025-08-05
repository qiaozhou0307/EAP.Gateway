using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using EAP.Gateway.Core.Events.Common;

namespace EAP.Gateway.Core.Events.Alarm;

/// <summary>
/// 报警确认事件
/// </summary>
public sealed class AlarmAcknowledgedEvent : DomainEventBase
{
    public EquipmentId EquipmentId { get; }
    public ushort AlarmId { get; }
    public string AcknowledgedBy { get; }
    public DateTime AcknowledgedAt { get; }

    public AlarmAcknowledgedEvent(EquipmentId equipmentId, ushort alarmId, string acknowledgedBy, DateTime acknowledgedAt)
    {
        EquipmentId = equipmentId;
        AlarmId = alarmId;
        AcknowledgedBy = acknowledgedBy;
        AcknowledgedAt = acknowledgedAt;
    }
}
