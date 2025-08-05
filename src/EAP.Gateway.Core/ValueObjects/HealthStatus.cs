namespace EAP.Gateway.Core.ValueObjects;

/// <summary>
/// 健康状态枚举
/// </summary>
public enum HealthStatus
{
    Unknown = 0,
    Healthy = 1,
    Degraded = 2,
    Unhealthy = 3
}
