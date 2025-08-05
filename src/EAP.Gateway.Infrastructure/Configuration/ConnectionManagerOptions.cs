namespace EAP.Gateway.Infrastructure.Configuration;

/// <summary>
/// 连接管理器配置选项
/// </summary>
public class ConnectionManagerOptions
{
    public const string SectionName = "ConnectionManager";

    public int MaxConcurrentConnections { get; set; } = 10;
    public int ConnectionRequestQueueSize { get; set; } = 100;
    public int HealthCheckIntervalSeconds { get; set; } = 30;
    public int CleanupIntervalMinutes { get; set; } = 5;
    public int StaleConnectionThresholdMinutes { get; set; } = 15;
    public int ConnectionTimeoutSeconds { get; set; } = 30;
    public int ReconnectionMaxAttempts { get; set; } = 3;
    public int ReconnectionDelaySeconds { get; set; } = 5;
}
