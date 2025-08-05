using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using EAP.Gateway.Core.ValueObjects;

namespace EAP.Gateway.Core.Models;

/// <summary>
/// 设备健康检查结果
/// </summary>
public class DeviceHealthCheckResult
{
    /// <summary>
    /// 设备标识
    /// </summary>
    public EquipmentId EquipmentId { get; set; } = null!;

    /// <summary>
    /// 整体健康状态
    /// </summary>
    public HealthStatus OverallHealth { get; set; }

    /// <summary>
    /// 检查项目列表
    /// </summary>
    public IReadOnlyList<HealthCheckItem> CheckItems { get; set; } = new List<HealthCheckItem>().AsReadOnly();

    /// <summary>
    /// 检查时间
    /// </summary>
    public DateTime CheckTime { get; set; }

    /// <summary>
    /// 检查耗时
    /// </summary>
    public TimeSpan CheckDuration { get; set; }

    /// <summary>
    /// 私有构造函数
    /// </summary>
    public DeviceHealthCheckResult() { }

    /// <summary>
    /// 创建健康检查结果
    /// </summary>
    /// <param name="equipmentId">设备标识</param>
    /// <param name="overallHealth">整体健康状态</param>
    /// <param name="checkItems">检查项目</param>
    /// <param name="checkDuration">检查耗时</param>
    /// <returns>健康检查结果</returns>
    public static DeviceHealthCheckResult Create(
        EquipmentId equipmentId,
        HealthStatus overallHealth,
        IEnumerable<HealthCheckItem> checkItems,
        TimeSpan? checkDuration = null)
    {
        return new DeviceHealthCheckResult
        {
            EquipmentId = equipmentId,
            OverallHealth = overallHealth,
            CheckItems = checkItems.ToList().AsReadOnly(),
            CheckTime = DateTime.UtcNow,
            CheckDuration = checkDuration ?? TimeSpan.Zero
        };
    }

    /// <summary>
    /// 是否健康
    /// </summary>
    public bool IsHealthy => OverallHealth == HealthStatus.Healthy;

    /// <summary>
    /// 是否需要关注
    /// </summary>
    public bool RequiresAttention => OverallHealth == HealthStatus.Unhealthy || OverallHealth == HealthStatus.Degraded;

    /// <summary>
    /// 获取失败的检查项
    /// </summary>
    public IEnumerable<HealthCheckItem> FailedChecks => CheckItems.Where(item => item.Status != HealthStatus.Healthy);

    /// <summary>
    /// 获取关键失败的检查项
    /// </summary>
    public IEnumerable<HealthCheckItem> CriticalFailures => CheckItems.Where(item => item.Status == HealthStatus.Unhealthy);
}
