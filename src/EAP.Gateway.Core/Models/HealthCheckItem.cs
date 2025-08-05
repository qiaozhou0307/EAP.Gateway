using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using EAP.Gateway.Core.ValueObjects;

namespace EAP.Gateway.Core.Models;

/// <summary>
/// 健康检查项
/// </summary>
public class HealthCheckItem
{
    /// <summary>
    /// 检查项名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 检查项类别
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// 健康状态
    /// </summary>
    public HealthStatus Status { get; set; }

    /// <summary>
    /// 检查消息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 检查时间
    /// </summary>
    public DateTime CheckedAt { get; set; }

    /// <summary>
    /// 检查详情
    /// </summary>
    public IDictionary<string, object>? Details { get; set; }

    /// <summary>
    /// 检查耗时
    /// </summary>
    public TimeSpan? Duration { get; set; }

    /// <summary>
    /// 私有构造函数
    /// </summary>
    private HealthCheckItem() { }

    /// <summary>
    /// 创建健康检查项
    /// </summary>
    /// <param name="name">检查项名称</param>
    /// <param name="category">检查项类别</param>
    /// <param name="status">健康状态</param>
    /// <param name="message">检查消息</param>
    /// <param name="details">检查详情</param>
    /// <param name="duration">检查耗时</param>
    /// <returns>健康检查项</returns>
    public static HealthCheckItem Create(
        string name,
        string category,
        HealthStatus status,
        string message,
        IDictionary<string, object>? details = null,
        TimeSpan? duration = null)
    {
        return new HealthCheckItem
        {
            Name = name,
            Category = category,
            Status = status,
            Message = message,
            CheckedAt = DateTime.UtcNow,
            Details = details,
            Duration = duration
        };
    }

    /// <summary>
    /// 创建成功的检查项
    /// </summary>
    /// <param name="name">检查项名称</param>
    /// <param name="category">检查项类别</param>
    /// <param name="message">检查消息</param>
    /// <returns>健康检查项</returns>
    public static HealthCheckItem CreateHealthy(string name, string category, string message = "OK")
    {
        return Create(name, category, HealthStatus.Healthy, message);
    }

    /// <summary>
    /// 创建降级的检查项
    /// </summary>
    /// <param name="name">检查项名称</param>
    /// <param name="category">检查项类别</param>
    /// <param name="message">检查消息</param>
    /// <returns>健康检查项</returns>
    public static HealthCheckItem CreateDegraded(string name, string category, string message)
    {
        return Create(name, category, HealthStatus.Degraded, message);
    }

    /// <summary>
    /// 创建不健康的检查项
    /// </summary>
    /// <param name="name">检查项名称</param>
    /// <param name="category">检查项类别</param>
    /// <param name="message">检查消息</param>
    /// <returns>健康检查项</returns>
    public static HealthCheckItem CreateUnhealthy(string name, string category, string message)
    {
        return Create(name, category, HealthStatus.Unhealthy, message);
    }

    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccessful => Status == HealthStatus.Healthy;

    /// <summary>
    /// 是否失败
    /// </summary>
    public bool IsFailed => Status == HealthStatus.Unhealthy;

    /// <summary>
    /// 是否需要关注
    /// </summary>
    public bool RequiresAttention => Status != HealthStatus.Healthy;

    public override string ToString()
    {
        return $"{Name} ({Category}): {Status} - {Message}";
    }
}
