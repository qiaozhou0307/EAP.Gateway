using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using EAP.Gateway.Core.Entities;
using EAP.Gateway.Core.Events.Common;

namespace EAP.Gateway.Core.Events.Alarm;

/// <summary>
/// 报警清除事件
/// </summary>
public sealed class AlarmClearedEvent : DomainEventBase
{
    public EquipmentId EquipmentId { get; }
    public AlarmEvent ClearedAlarm { get; }
    public string? ClearReason { get; }
    public DateTime ClearedAt { get; }
    public string? ClearedBy { get; }

    public AlarmClearedEvent(EquipmentId equipmentId, AlarmEvent clearedAlarm, string? clearReason, DateTime clearedAt, string? clearedBy = null)
    {
        EquipmentId = equipmentId;
        ClearedAlarm = clearedAlarm;
        ClearReason = clearReason;
        ClearedAt = clearedAt;
        ClearedBy = clearedBy;
    }
}
