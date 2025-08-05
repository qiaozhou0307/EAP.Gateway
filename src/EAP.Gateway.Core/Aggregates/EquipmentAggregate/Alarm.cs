using EAP.Gateway.Core.Common;
using EAP.Gateway.Core.ValueObjects;

namespace EAP.Gateway.Core.Aggregates.EquipmentAggregate;

/// <summary>
/// 报警实体 - 使用现有Entity基类
/// </summary>
public class Alarm : Entity<AlarmId>
{
    public EquipmentId EquipmentId { get; private set; }
    public string AlarmCode { get; private set; }
    public string AlarmText { get; private set; }
    public AlarmSeverity Severity { get; private set; }
    public AlarmState State { get; private set; }
    public DateTime OccurredAt { get; private set; }
    public DateTime? ClearedAt { get; private set; }

    private Alarm() : base(AlarmId.Create()) { } // EF Core

    public Alarm(AlarmId id, EquipmentId equipmentId, string alarmCode, string alarmText, AlarmSeverity severity)
        : base(id)
    {
        EquipmentId = equipmentId;
        AlarmCode = alarmCode;
        AlarmText = alarmText;
        Severity = severity;
        State = AlarmState.Active;
        OccurredAt = DateTime.UtcNow;
    }

    public void Clear()
    {
        State = AlarmState.Cleared;
        ClearedAt = DateTime.UtcNow;
    }
}
