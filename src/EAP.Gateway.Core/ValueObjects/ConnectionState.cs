using EAP.Gateway.Core.Common;

namespace EAP.Gateway.Core.ValueObjects;

/// <summary>
/// 连接状态值对象
/// </summary>
public class ConnectionState : ValueObject
{
    /// <summary>
    /// 是否已连接
    /// </summary>
    public bool IsConnected { get; }

    /// <summary>
    /// 最后连接时间
    /// </summary>
    public DateTime? LastConnectedAt { get; }

    /// <summary>
    /// 最后断开连接时间
    /// </summary>
    public DateTime? LastDisconnectedAt { get; }

    /// <summary>
    /// 断开连接原因
    /// </summary>
    public string? DisconnectReason { get; }

    /// <summary>
    /// 重试次数
    /// </summary>
    public int RetryCount { get; }

    /// <summary>
    /// 最大重试次数
    /// </summary>
    public int MaxRetries { get; }

    /// <summary>
    /// 会话ID
    /// </summary>
    public string? SessionId { get; }

    /// <summary>
    /// 连接质量
    /// </summary>
    public ConnectionQuality Quality { get; }

    /// <summary>
    /// 最后心跳时间
    /// </summary>
    public DateTime? LastHeartbeatAt { get; }

    /// <summary>
    /// 连接开始时间
    /// </summary>
    public DateTime? ConnectionStartedAt { get; }

    /// <summary>
    /// 连接持续时间
    /// </summary>
    public TimeSpan? ConnectionDuration => IsConnected && ConnectionStartedAt.HasValue
        ? DateTime.UtcNow - ConnectionStartedAt.Value
        : LastDisconnectedAt - ConnectionStartedAt;

    private ConnectionState(
        bool isConnected,
        DateTime? lastConnectedAt,
        DateTime? lastDisconnectedAt,
        string? disconnectReason,
        int retryCount,
        int maxRetries,
        string? sessionId,
        ConnectionQuality quality,
        DateTime? lastHeartbeatAt,
        DateTime? connectionStartedAt)
    {
        IsConnected = isConnected;
        LastConnectedAt = lastConnectedAt;
        LastDisconnectedAt = lastDisconnectedAt;
        DisconnectReason = disconnectReason;
        RetryCount = Math.Max(0, retryCount);
        MaxRetries = Math.Max(0, maxRetries);
        SessionId = sessionId;
        Quality = quality;
        LastHeartbeatAt = lastHeartbeatAt;
        ConnectionStartedAt = connectionStartedAt;
    }

    /// <summary>
    /// 创建初始连接状态
    /// </summary>
    /// <param name="maxRetries">最大重试次数</param>
    /// <returns>初始连接状态</returns>
    public static ConnectionState Initial(int maxRetries = 3)
    {
        return Disconnected(maxRetries);
    }

    /// <summary>
    /// 创建断开连接状态
    /// </summary>
    /// <param name="maxRetries">最大重试次数</param>
    /// <returns>断开连接状态</returns>
    public static ConnectionState Disconnected(int maxRetries = 3)
    {
        return new ConnectionState(
            isConnected: false,
            lastConnectedAt: null,
            lastDisconnectedAt: null,
            disconnectReason: null,
            retryCount: 0,
            maxRetries: maxRetries,
            sessionId: null,
            quality: ConnectionQuality.Unknown,
            lastHeartbeatAt: null,
            connectionStartedAt: null);
    }

    /// <summary>
    /// 创建已连接状态
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <param name="connectionTime">连接时间</param>
    /// <returns>已连接状态</returns>
    public static ConnectionState Connected(string? sessionId = null, DateTime? connectionTime = null)
    {
        var connectedAt = connectionTime ?? DateTime.UtcNow;
        return new ConnectionState(
            isConnected: true,
            lastConnectedAt: connectedAt,
            lastDisconnectedAt: null,
            disconnectReason: null,
            retryCount: 0,
            maxRetries: 3,
            sessionId: sessionId,
            quality: ConnectionQuality.Good,
            lastHeartbeatAt: connectedAt,
            connectionStartedAt: connectedAt);
    }

    /// <summary>
    /// 连接成功
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <param name="connectionTime">连接时间</param>
    /// <returns>新的连接状态</returns>
    public ConnectionState Connect(string? sessionId = null, DateTime? connectionTime = null)
    {
        var connectedAt = connectionTime ?? DateTime.UtcNow;
        return new ConnectionState(
            isConnected: true,
            lastConnectedAt: connectedAt,
            lastDisconnectedAt: LastDisconnectedAt,
            disconnectReason: null,
            retryCount: 0, // 连接成功后重置重试次数
            maxRetries: MaxRetries,
            sessionId: sessionId,
            quality: ConnectionQuality.Good,
            lastHeartbeatAt: connectedAt,
            connectionStartedAt: connectedAt);
    }

    /// <summary>
    /// 断开连接
    /// </summary>
    /// <param name="reason">断开原因</param>
    /// <param name="disconnectionTime">断开时间</param>
    /// <returns>新的连接状态</returns>
    public ConnectionState Disconnect(string? reason = null, DateTime? disconnectionTime = null)
    {
        var disconnectedAt = disconnectionTime ?? DateTime.UtcNow;
        return new ConnectionState(
            isConnected: false,
            lastConnectedAt: LastConnectedAt,
            lastDisconnectedAt: disconnectedAt,
            disconnectReason: reason,
            retryCount: RetryCount,
            maxRetries: MaxRetries,
            sessionId: null,
            quality: ConnectionQuality.Poor,
            lastHeartbeatAt: LastHeartbeatAt,
            connectionStartedAt: ConnectionStartedAt);
    }

    /// <summary>
    /// 重试连接
    /// </summary>
    /// <returns>新的连接状态</returns>
    public ConnectionState Retry()
    {
        return new ConnectionState(
            isConnected: false,
            lastConnectedAt: LastConnectedAt,
            lastDisconnectedAt: LastDisconnectedAt,
            disconnectReason: DisconnectReason,
            retryCount: RetryCount + 1,
            maxRetries: MaxRetries,
            sessionId: SessionId,
            quality: Quality,
            lastHeartbeatAt: LastHeartbeatAt,
            connectionStartedAt: ConnectionStartedAt);
    }

    /// <summary>
    /// 更新心跳
    /// </summary>
    /// <param name="heartbeatTime">心跳时间</param>
    /// <returns>新的连接状态</returns>
    public ConnectionState UpdateHeartbeat(DateTime? heartbeatTime = null)
    {
        if (!IsConnected)
            return this;

        var heartbeat = heartbeatTime ?? DateTime.UtcNow;
        var newQuality = CalculateQuality(heartbeat);

        return new ConnectionState(
            isConnected: IsConnected,
            lastConnectedAt: LastConnectedAt,
            lastDisconnectedAt: LastDisconnectedAt,
            disconnectReason: DisconnectReason,
            retryCount: RetryCount,
            maxRetries: MaxRetries,
            sessionId: SessionId,
            quality: newQuality,
            lastHeartbeatAt: heartbeat,
            connectionStartedAt: ConnectionStartedAt);
    }

    /// <summary>
    /// 重置重试次数
    /// </summary>
    /// <returns>新的连接状态</returns>
    public ConnectionState ResetRetryCount()
    {
        return new ConnectionState(
            isConnected: IsConnected,
            lastConnectedAt: LastConnectedAt,
            lastDisconnectedAt: LastDisconnectedAt,
            disconnectReason: DisconnectReason,
            retryCount: 0,
            maxRetries: MaxRetries,
            sessionId: SessionId,
            quality: Quality,
            lastHeartbeatAt: LastHeartbeatAt,
            connectionStartedAt: ConnectionStartedAt);
    }

    /// <summary>
    /// 检查是否需要重连（实例属性）
    /// </summary>
    public bool RequiresReconnection => !IsConnected && RetryCount < MaxRetries;

    /// <summary>
    /// 检查是否可以重试
    /// </summary>
    public bool CanRetry => !IsConnected && RetryCount < MaxRetries;

    /// <summary>
    /// 检查连接是否稳定
    /// </summary>
    public bool IsStable => IsConnected && Quality >= ConnectionQuality.Good;

    /// <summary>
    /// 检查是否超时
    /// </summary>
    /// <param name="timeoutDuration">超时时长</param>
    /// <returns>是否超时</returns>
    public bool IsTimeout(TimeSpan timeoutDuration)
    {
        if (!IsConnected || !LastHeartbeatAt.HasValue)
            return false;

        return DateTime.UtcNow - LastHeartbeatAt.Value > timeoutDuration;
    }

    /// <summary>
    /// 计算连接质量
    /// </summary>
    private ConnectionQuality CalculateQuality(DateTime heartbeatTime)
    {
        if (!LastHeartbeatAt.HasValue)
            return ConnectionQuality.Good;

        var timeSinceLastHeartbeat = heartbeatTime - LastHeartbeatAt.Value;

        return timeSinceLastHeartbeat.TotalSeconds switch
        {
            <= 30 => ConnectionQuality.Excellent,
            <= 60 => ConnectionQuality.Good,
            <= 120 => ConnectionQuality.Fair,
            _ => ConnectionQuality.Poor
        };
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return IsConnected;
        yield return LastConnectedAt ?? DateTime.MinValue;
        yield return LastDisconnectedAt ?? DateTime.MinValue;
        yield return DisconnectReason ?? string.Empty;
        yield return RetryCount;
        yield return MaxRetries;
        yield return SessionId ?? string.Empty;
        yield return Quality;
        yield return LastHeartbeatAt ?? DateTime.MinValue;
        yield return ConnectionStartedAt ?? DateTime.MinValue;
    }

    public override string ToString()
    {
        var status = IsConnected ? "Connected" : "Disconnected";
        var qualityStr = IsConnected ? $" ({Quality})" : string.Empty;
        var retryStr = !IsConnected && RetryCount > 0 ? $" [Retry: {RetryCount}/{MaxRetries}]" : string.Empty;
        return $"{status}{qualityStr}{retryStr}";
    }
}
