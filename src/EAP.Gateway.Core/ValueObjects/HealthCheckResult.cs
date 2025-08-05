using EAP.Gateway.Core.Aggregates.EquipmentAggregate;

namespace EAP.Gateway.Core.ValueObjects;

/// <summary>
/// 健康检查结果
/// </summary>
public class HealthCheckResult
{
    /// <summary>
    /// 健康状态
    /// </summary>
    public HealthStatus Status { get; }

    /// <summary>
    /// 检查时间
    /// </summary>
    public DateTime CheckedAt { get; }

    /// <summary>
    /// 问题列表
    /// </summary>
    public IReadOnlyList<string> Issues { get; }

    /// <summary>
    /// 检查详情
    /// </summary>
    public IReadOnlyDictionary<string, object>? Details { get; }

    /// <summary>
    /// 检查耗时
    /// </summary>
    public TimeSpan Duration { get; }

    public HealthCheckResult(
        HealthStatus status,
        IEnumerable<string>? issues = null,
        IReadOnlyDictionary<string, object>? details = null,
        TimeSpan? duration = null)
    {
        Status = status;
        CheckedAt = DateTime.UtcNow;
        Issues = issues?.ToList().AsReadOnly() ?? new List<string>().AsReadOnly();
        Details = details;
        Duration = duration ?? TimeSpan.Zero;
    }

    /// <summary>
    /// 创建健康结果
    /// </summary>
    public static HealthCheckResult Healthy(
        IReadOnlyDictionary<string, object>? details = null,
        TimeSpan? duration = null)
    {
        return new HealthCheckResult(HealthStatus.Healthy, null, details, duration);
    }

    /// <summary>
    /// 创建不健康结果
    /// </summary>
    public static HealthCheckResult Unhealthy(
        IEnumerable<string> issues,
        IReadOnlyDictionary<string, object>? details = null,
        TimeSpan? duration = null)
    {
        return new HealthCheckResult(HealthStatus.Unhealthy, issues, details, duration);
    }

    /// <summary>
    /// 创建降级结果
    /// </summary>
    public static HealthCheckResult Degraded(
        IEnumerable<string> issues,
        IReadOnlyDictionary<string, object>? details = null,
        TimeSpan? duration = null)
    {
        return new HealthCheckResult(HealthStatus.Degraded, issues, details, duration);
    }

    /// <summary>
    /// 是否健康
    /// </summary>
    public bool IsHealthy => Status == HealthStatus.Healthy;

    /// <summary>
    /// 是否需要关注
    /// </summary>
    public bool RequiresAttention => Status == HealthStatus.Unhealthy || Status == HealthStatus.Degraded;
}
