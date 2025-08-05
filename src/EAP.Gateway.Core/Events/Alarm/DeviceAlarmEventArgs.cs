using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using EAP.Gateway.Core.Entities;
using EAP.Gateway.Core.ValueObjects;

namespace EAP.Gateway.Core.Events.Alarm;

/// <summary>
/// 设备报警事件参数
/// </summary>
public class DeviceAlarmEventArgs : EventArgs
{
    /// <summary>
    /// 设备ID
    /// </summary>
    public EquipmentId EquipmentId { get; }

    /// <summary>
    /// 报警事件
    /// </summary>
    public AlarmEvent AlarmEvent { get; }

    /// <summary>
    /// 报警类型
    /// </summary>
    public string AlarmType { get; }

    /// <summary>
    /// 事件发生时间
    /// </summary>
    public DateTime OccurredAt { get; }

    public DeviceAlarmEventArgs(
        EquipmentId equipmentId,
        AlarmEvent alarmEvent,
        string alarmType)
    {
        EquipmentId = equipmentId;
        AlarmEvent = alarmEvent;
        AlarmType = alarmType;
        OccurredAt = DateTime.UtcNow;
    }
}
