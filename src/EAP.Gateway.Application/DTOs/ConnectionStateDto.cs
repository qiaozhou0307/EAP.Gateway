namespace EAP.Gateway.Application.DTOs;

/// <summary>
/// 连接状态DTO
/// </summary>
public class ConnectionStateDto
{
    /// <summary>
    /// 是否已连接
    /// </summary>
    public bool IsConnected { get; set; }

    /// <summary>
    /// 连接状态文本
    /// </summary>
    public required string Status { get; set; }

    /// <summary>
    /// 最后连接时间
    /// </summary>
    public DateTime? LastConnectedAt { get; set; }

    /// <summary>
    /// 最后断开时间
    /// </summary>
    public DateTime? LastDisconnectedAt { get; set; }

    /// <summary>
    /// 断开原因
    /// </summary>
    public string? DisconnectReason { get; set; }

    /// <summary>
    /// 重试次数
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// 会话ID
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// IP地址
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// 端口号
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// 连接稳定性
    /// </summary>
    public bool IsStable => IsConnected && RetryCount < 3;

    /// <summary>
    /// 连接时长
    /// </summary>
    public TimeSpan? ConnectionDuration => IsConnected && LastConnectedAt.HasValue
        ? DateTime.UtcNow - LastConnectedAt.Value
        : null;

    /// <summary>
    /// 质量
    /// </summary>
    public string Quality { get; set; } = string.Empty;

    /// <summary>
    /// 最后心跳时间
    /// </summary>
    public DateTime? LastHeartbeatAt { get; set; }

    /// <summary>
    /// 状态显示颜色
    /// </summary>
    public string StatusColor => Status switch
    {
        "Connected" => IsStable ? "green" : "yellow",
        "Connecting" => "blue",
        "Disconnected" => "gray",
        "Failed" => "red",
        _ => "gray"
    };

    /// <summary>
    /// 创建已连接状态
    /// </summary>
    /// <param name="ipAddress">IP地址</param>
    /// <param name="port">端口</param>
    /// <param name="sessionId">会话ID</param>
    /// <returns>已连接状态DTO</returns>
    public static ConnectionStateDto CreateConnected(string ipAddress, int port, string? sessionId = null)
    {
        return new ConnectionStateDto
        {
            IsConnected = true,
            Status = "Connected",
            LastConnectedAt = DateTime.UtcNow,
            IpAddress = ipAddress,
            Port = port,
            SessionId = sessionId,
            RetryCount = 0
        };
    }

    /// <summary>
    /// 创建断开状态
    /// </summary>
    /// <param name="reason">断开原因</param>
    /// <returns>断开状态DTO</returns>
    public static ConnectionStateDto CreateDisconnected(string? reason = null)
    {
        return new ConnectionStateDto
        {
            IsConnected = false,
            Status = "Disconnected",
            LastDisconnectedAt = DateTime.UtcNow,
            DisconnectReason = reason
        };
    }
}
