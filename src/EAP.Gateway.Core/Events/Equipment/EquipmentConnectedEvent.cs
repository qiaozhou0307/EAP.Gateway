using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using EAP.Gateway.Core.Events.Common;
using EAP.Gateway.Core.ValueObjects;

namespace EAP.Gateway.Core.Events.Equipment;

/// <summary>
/// 设备连接成功事件
/// 当设备成功建立HSMS连接时发布
/// </summary>
public sealed class EquipmentConnectedEvent : DomainEventBase
{
    /// <summary>
    /// 设备标识
    /// </summary>
    public EquipmentId EquipmentId { get; }

    /// <summary>
    /// 连接建立时间
    /// </summary>
    public DateTime ConnectedAt { get; }

    /// <summary>
    /// 连接会话ID
    /// </summary>
    public string SessionId { get; }

    /// <summary>
    /// 设备网络端点
    /// </summary>
    public IpEndpoint Endpoint { get; }

    /// <summary>
    /// 连接尝试次数
    /// </summary>
    public int AttemptNumber { get; }

    /// <summary>
    /// 连接建立耗时（毫秒）
    /// </summary>
    public long ConnectionDurationMs { get; }

    /// <summary>
    /// 是否为重连
    /// </summary>
    public bool IsReconnection { get; }

    /// <summary>
    /// 连接前的设备状态
    /// </summary>
    public EquipmentState? PreviousState { get; }

    /// <summary>
    /// 附加的连接信息
    /// </summary>
    public IDictionary<string, object>? AdditionalInfo { get; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="equipmentId">设备标识</param>
    /// <param name="connectedAt">连接时间</param>
    /// <param name="sessionId">会话ID</param>
    /// <param name="endpoint">网络端点</param>
    /// <param name="attemptNumber">连接尝试次数</param>
    /// <param name="connectionDurationMs">连接耗时</param>
    /// <param name="isReconnection">是否重连</param>
    /// <param name="previousState">之前状态</param>
    /// <param name="additionalInfo">附加信息</param>
    public EquipmentConnectedEvent(
        EquipmentId equipmentId,
        DateTime connectedAt,
        string sessionId,
        IpEndpoint endpoint,
        int attemptNumber = 1,
        long connectionDurationMs = 0,
        bool isReconnection = false,
        EquipmentState? previousState = null,
        IDictionary<string, object>? additionalInfo = null)
    {
        EquipmentId = equipmentId ?? throw new ArgumentNullException(nameof(equipmentId));
        ConnectedAt = connectedAt;
        SessionId = sessionId ?? throw new ArgumentNullException(nameof(sessionId));
        Endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        AttemptNumber = attemptNumber > 0 ? attemptNumber : throw new ArgumentOutOfRangeException(nameof(attemptNumber));
        ConnectionDurationMs = connectionDurationMs >= 0 ? connectionDurationMs : throw new ArgumentOutOfRangeException(nameof(connectionDurationMs));
        IsReconnection = isReconnection;
        PreviousState = previousState;
        AdditionalInfo = additionalInfo != null ? new Dictionary<string, object>(additionalInfo) : null;
    }

    /// <summary>
    /// 创建连接事件的简化工厂方法
    /// </summary>
    /// <param name="equipmentId">设备标识</param>
    /// <param name="sessionId">会话ID</param>
    /// <param name="endpoint">网络端点</param>
    /// <returns>设备连接事件</returns>
    public static EquipmentConnectedEvent Create(EquipmentId equipmentId, string sessionId, IpEndpoint endpoint)
    {
        return new EquipmentConnectedEvent(equipmentId, DateTime.UtcNow, sessionId, endpoint);
    }

    /// <summary>
    /// 创建重连事件的工厂方法
    /// </summary>
    /// <param name="equipmentId">设备标识</param>
    /// <param name="sessionId">会话ID</param>
    /// <param name="endpoint">网络端点</param>
    /// <param name="attemptNumber">重连尝试次数</param>
    /// <param name="connectionDurationMs">连接耗时</param>
    /// <returns>设备重连事件</returns>
    public static EquipmentConnectedEvent CreateReconnection(
        EquipmentId equipmentId,
        string sessionId,
        IpEndpoint endpoint,
        int attemptNumber,
        long connectionDurationMs)
    {
        return new EquipmentConnectedEvent(
            equipmentId, DateTime.UtcNow, sessionId, endpoint,
            attemptNumber, connectionDurationMs, true);
    }

    public override string ToString()
    {
        var reconnectionInfo = IsReconnection ? $" (Reconnection #{AttemptNumber})" : "";
        return $"Equipment {EquipmentId} connected to {Endpoint}{reconnectionInfo} [Session: {SessionId}]";
    }
}
