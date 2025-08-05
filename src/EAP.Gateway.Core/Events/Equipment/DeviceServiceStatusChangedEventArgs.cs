using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using EAP.Gateway.Core.ValueObjects;

namespace EAP.Gateway.Core.Events.Equipment;

/// <summary>
/// 设备服务状态变更事件参数
/// </summary>
public class DeviceServiceStatusChangedEventArgs : EventArgs
{
    /// <summary>
    /// 设备ID
    /// </summary>
    public EquipmentId EquipmentId { get; }

    /// <summary>
    /// 旧状态
    /// </summary>
    public DeviceServiceStatus OldStatus { get; }

    /// <summary>
    /// 新状态
    /// </summary>
    public DeviceServiceStatus NewStatus { get; }

    /// <summary>
    /// 状态变更时间
    /// </summary>
    public DateTime ChangedAt { get; }

    /// <summary>
    /// 变更原因
    /// </summary>
    public string? Reason { get; }

    /// <summary>
    /// 异常信息（如果有）
    /// </summary>
    public Exception? Exception { get; }

    public DeviceServiceStatusChangedEventArgs(
        EquipmentId equipmentId,
        DeviceServiceStatus oldStatus,
        DeviceServiceStatus newStatus,
        string? reason = null,
        Exception? exception = null)
    {
        EquipmentId = equipmentId;
        OldStatus = oldStatus;
        NewStatus = newStatus;
        ChangedAt = DateTime.UtcNow;
        Reason = reason;
        Exception = exception;
    }
}
