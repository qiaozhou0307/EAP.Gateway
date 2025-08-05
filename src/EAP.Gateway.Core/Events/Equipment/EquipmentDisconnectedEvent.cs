using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using EAP.Gateway.Core.Events.Common;
using EAP.Gateway.Core.ValueObjects;

namespace EAP.Gateway.Core.Events.Equipment;

/// <summary>
/// 设备断开连接事件
/// 当设备HSMS连接断开时发布
/// </summary>
public sealed class EquipmentDisconnectedEvent : DomainEventBase
{
    /// <summary>
    /// 设备标识
    /// </summary>
    public EquipmentId EquipmentId { get; }

    /// <summary>
    /// 断开连接时间
    /// </summary>
    public DateTime DisconnectedAt { get; }

    /// <summary>
    /// 断开连接原因
    /// </summary>
    public string? Reason { get; }

    /// <summary>
    /// 断开连接类型
    /// </summary>
    public DisconnectionType DisconnectionType { get; }

    /// <summary>
    /// 之前的会话ID
    /// </summary>
    public string? PreviousSessionId { get; }

    /// <summary>
    /// 连接持续时间
    /// </summary>
    public TimeSpan? ConnectionDuration { get; }

    /// <summary>
    /// 断开前的设备状态
    /// </summary>
    public EquipmentState? PreviousState { get; }

    /// <summary>
    /// 是否预期的断开（如手动断开）
    /// </summary>
    public bool IsExpectedDisconnection { get; }

    /// <summary>
    /// 是否需要自动重连
    /// </summary>
    public bool RequiresReconnection { get; }

    /// <summary>
    /// 附加的断开信息
    /// </summary>
    public IDictionary<string, object>? AdditionalInfo { get; }

    /// <summary>
    /// 构造函数
    /// </summary>
    public EquipmentDisconnectedEvent(
        EquipmentId equipmentId,
        DateTime disconnectedAt,
        string? reason = null,
        DisconnectionType disconnectionType = DisconnectionType.Unexpected,
        string? previousSessionId = null,
        TimeSpan? connectionDuration = null,
        EquipmentState? previousState = null,
        bool isExpectedDisconnection = false,
        bool requiresReconnection = true,
        IDictionary<string, object>? additionalInfo = null)
    {
        EquipmentId = equipmentId ?? throw new ArgumentNullException(nameof(equipmentId));
        DisconnectedAt = disconnectedAt;
        Reason = reason;
        DisconnectionType = disconnectionType;
        PreviousSessionId = previousSessionId;
        ConnectionDuration = connectionDuration;
        PreviousState = previousState;
        IsExpectedDisconnection = isExpectedDisconnection;
        RequiresReconnection = requiresReconnection;
        AdditionalInfo = additionalInfo != null ? new Dictionary<string, object>(additionalInfo) : null;
    }

    /// <summary>
    /// 创建意外断开事件
    /// </summary>
    /// <param name="equipmentId">设备标识</param>
    /// <param name="reason">断开原因</param>
    /// <param name="previousSessionId">之前会话ID</param>
    /// <returns>断开连接事件</returns>
    public static EquipmentDisconnectedEvent CreateUnexpected(
        EquipmentId equipmentId,
        string reason,
        string? previousSessionId = null)
    {
        return new EquipmentDisconnectedEvent(
            equipmentId, DateTime.UtcNow, reason,
            DisconnectionType.Unexpected, previousSessionId,
            requiresReconnection: true);
    }

    /// <summary>
    /// 创建手动断开事件
    /// </summary>
    /// <param name="equipmentId">设备标识</param>
    /// <param name="previousSessionId">之前会话ID</param>
    /// <param name="connectionDuration">连接持续时间</param>
    /// <returns>断开连接事件</returns>
    public static EquipmentDisconnectedEvent CreateManual(
        EquipmentId equipmentId,
        string? previousSessionId = null,
        TimeSpan? connectionDuration = null)
    {
        return new EquipmentDisconnectedEvent(
            equipmentId, DateTime.UtcNow, "Manual disconnection",
            DisconnectionType.Manual, previousSessionId, connectionDuration,
            isExpectedDisconnection: true, requiresReconnection: false);
    }

    /// <summary>
    /// 创建网络错误断开事件
    /// </summary>
    /// <param name="equipmentId">设备标识</param>
    /// <param name="networkError">网络错误信息</param>
    /// <param name="previousSessionId">之前会话ID</param>
    /// <returns>断开连接事件</returns>
    public static EquipmentDisconnectedEvent CreateNetworkError(
        EquipmentId equipmentId,
        string networkError,
        string? previousSessionId = null)
    {
        return new EquipmentDisconnectedEvent(
            equipmentId, DateTime.UtcNow, $"Network error: {networkError}",
            DisconnectionType.NetworkError, previousSessionId,
            requiresReconnection: true);
    }

    public override string ToString()
    {
        var reasonInfo = !string.IsNullOrEmpty(Reason) ? $" - {Reason}" : "";
        var durationInfo = ConnectionDuration?.ToString(@"hh\:mm\:ss") ?? "unknown";
        return $"Equipment {EquipmentId} disconnected ({DisconnectionType}) after {durationInfo}{reasonInfo}";
    }
}
/// <summary>
/// 断开连接类型枚举
/// </summary>
public enum DisconnectionType
{
    /// <summary>
    /// 意外断开
    /// </summary>
    Unexpected = 0,

    /// <summary>
    /// 手动断开
    /// </summary>
    Manual = 1,

    /// <summary>
    /// 网络错误
    /// </summary>
    NetworkError = 2,

    /// <summary>
    /// 协议错误
    /// </summary>
    ProtocolError = 3,

    /// <summary>
    /// 超时断开
    /// </summary>
    Timeout = 4,

    /// <summary>
    /// 设备主动断开
    /// </summary>
    DeviceInitiated = 5,

    /// <summary>
    /// 系统关闭
    /// </summary>
    SystemShutdown = 6
}
