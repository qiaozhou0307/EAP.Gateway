using EAP.Gateway.Core.Common;

namespace EAP.Gateway.Core.ValueObjects;

/// <summary>
/// 设备连接配置值对象
/// 支持FR-DAM-001需求：配置数据存储（设备连接配置）
/// </summary>
public class DeviceConnectionConfig : ValueObject
{
    public string IpAddress { get; }
    public int Port { get; }
    public int T3Timeout { get; }
    public int T5Timeout { get; }
    public int T6Timeout { get; }
    public int T7Timeout { get; }
    public int T8Timeout { get; }
    public int RetryCount { get; }
    public int RetryInterval { get; }
    public bool EnableAutoReconnect { get; }
    public int HeartbeatInterval { get; }
    public ConnectionMode ConnectionMode { get; }

    public DeviceConnectionConfig(
        string ipAddress,
        int port,
        int t3Timeout = 45,
        int t5Timeout = 10,
        int t6Timeout = 5,
        int t7Timeout = 10,
        int t8Timeout = 6,
        int retryCount = 3,
        int retryInterval = 5000,
        bool enableAutoReconnect = true,
        int heartbeatInterval = 30,
        ConnectionMode connectionMode = ConnectionMode.Active)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            throw new ArgumentException("IP address cannot be null or empty", nameof(ipAddress));

        if (port <= 0 || port > 65535)
            throw new ArgumentException("Port must be between 1 and 65535", nameof(port));

        IpAddress = ipAddress;
        Port = port;
        T3Timeout = t3Timeout;
        T5Timeout = t5Timeout;
        T6Timeout = t6Timeout;
        T7Timeout = t7Timeout;
        T8Timeout = t8Timeout;
        RetryCount = retryCount;
        RetryInterval = retryInterval;
        EnableAutoReconnect = enableAutoReconnect;
        HeartbeatInterval = heartbeatInterval;
        ConnectionMode = connectionMode;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return IpAddress;
        yield return Port;
        yield return T3Timeout;
        yield return T5Timeout;
        yield return T6Timeout;
        yield return T7Timeout;
        yield return T8Timeout;
        yield return RetryCount;
        yield return RetryInterval;
        yield return EnableAutoReconnect;
        yield return HeartbeatInterval;
        yield return ConnectionMode;
    }
}

public enum ConnectionMode
{
    Active,
    Passive
}
