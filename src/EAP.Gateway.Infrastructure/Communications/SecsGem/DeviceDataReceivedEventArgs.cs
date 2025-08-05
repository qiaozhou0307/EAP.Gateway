using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using EAP.Gateway.Core.ValueObjects;

namespace EAP.Gateway.Infrastructure.Communications.SecsGem;

/// <summary>
/// 设备数据接收事件参数
/// </summary>
public class DeviceDataReceivedEventArgs : EventArgs
{
    public EquipmentId EquipmentId { get; }
    public IReadOnlyDictionary<string, object> DataVariables { get; }
    public DateTime ReceivedAt { get; }
    public string? MessageType { get; }

    public DeviceDataReceivedEventArgs(
        EquipmentId equipmentId,
        IReadOnlyDictionary<string, object> dataVariables,
        DateTime receivedAt,
        string? messageType = null)
    {
        EquipmentId = equipmentId;
        DataVariables = dataVariables;
        ReceivedAt = receivedAt;
        MessageType = messageType;
    }
}

/// <summary>
/// 设备报警事件参数
/// </summary>
public class DeviceAlarmEventArgs : EventArgs
{
    public EquipmentId EquipmentId { get; }
    public uint AlarmId { get; }
    public string Message { get; }
    public AlarmSeverity Severity { get; }
    public DateTime OccurredAt { get; }

    public DeviceAlarmEventArgs(
        EquipmentId equipmentId,
        uint alarmId,
        string message,
        AlarmSeverity severity,
        DateTime occurredAt)
    {
        EquipmentId = equipmentId;
        AlarmId = alarmId;
        Message = message ?? string.Empty;
        Severity = severity;
        OccurredAt = occurredAt;
    }
}

/// <summary>
/// 设备服务状态变化事件参数
/// </summary>
public class DeviceServiceStatusChangedEventArgs : EventArgs
{
    public EquipmentId EquipmentId { get; }
    public DeviceServiceStatus PreviousStatus { get; }
    public DeviceServiceStatus NewStatus { get; }
    public string? Reason { get; }
    public DateTime ChangedAt { get; }

    public DeviceServiceStatusChangedEventArgs(
        EquipmentId equipmentId,
        DeviceServiceStatus previousStatus,
        DeviceServiceStatus newStatus,
        string? reason,
        DateTime changedAt)
    {
        EquipmentId = equipmentId;
        PreviousStatus = previousStatus;
        NewStatus = newStatus;
        Reason = reason;
        ChangedAt = changedAt;
    }
}

/// <summary>
/// 设备服务状态枚举
/// </summary>
public enum DeviceServiceStatus
{
    NotInitialized,
    Starting,
    Started,
    Stopping,
    Stopped,
    Faulted
}
