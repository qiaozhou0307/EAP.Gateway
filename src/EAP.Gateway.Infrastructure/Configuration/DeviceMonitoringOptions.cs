namespace EAP.Gateway.Infrastructure.Configuration;

/// <summary>
/// 设备监控配置选项
/// </summary>
public class DeviceMonitoringOptions
{
    public const string SectionName = "DeviceMonitoring";

    /// <summary>
    /// 是否启用设备监控
    /// </summary>
    public bool EnableMonitoring { get; set; } = true;

    /// <summary>
    /// 监控间隔（秒）
    /// </summary>
    public int MonitoringIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// 错误重试延迟（秒）
    /// </summary>
    public int ErrorRetryDelaySeconds { get; set; } = 10;

    /// <summary>
    /// 健康检查超时（秒）
    /// </summary>
    public int HealthCheckTimeoutSeconds { get; set; } = 5;
}
