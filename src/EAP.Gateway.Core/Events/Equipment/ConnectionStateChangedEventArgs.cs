using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using EAP.Gateway.Core.ValueObjects;

namespace EAP.Gateway.Core.Events.Equipment;

/// <summary>
/// 连接状态变更事件参数
/// </summary>
public class ConnectionStateChangedEventArgs : EventArgs
{
    /// <summary>
    /// 设备ID
    /// </summary>
    public EquipmentId EquipmentId { get; }

    /// <summary>
    /// 旧连接状态
    /// </summary>
    public ConnectionState OldState { get; }

    /// <summary>
    /// 新连接状态
    /// </summary>
    public ConnectionState NewState { get; }

    /// <summary>
    /// 变更时间
    /// </summary>
    public DateTime ChangedAt { get; }

    /// <summary>
    /// 变更原因
    /// </summary>
    public string? Reason { get; }

    /// <summary>
    /// 会话ID
    /// </summary>
    public string? SessionId { get; }

    public ConnectionStateChangedEventArgs(
        EquipmentId equipmentId,
        ConnectionState oldState,
        ConnectionState newState,
        string? reason = null,
        string? sessionId = null)
    {
        EquipmentId = equipmentId;
        OldState = oldState;
        NewState = newState;
        ChangedAt = DateTime.UtcNow;
        Reason = reason;
        SessionId = sessionId;
    }
}
