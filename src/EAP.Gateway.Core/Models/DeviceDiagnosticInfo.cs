using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using EAP.Gateway.Core.ValueObjects;

namespace EAP.Gateway.Core.Models;

/// <summary>
/// 设备诊断信息
/// </summary>
public class DeviceDiagnosticInfo
{
    /// <summary>
    /// 设备标识
    /// </summary>
    public EquipmentId EquipmentId { get; set; } = null!;

    /// <summary>
    /// 服务状态
    /// </summary>
    public DeviceServiceStatus ServiceStatus { get; set; }

    /// <summary>
    /// 连接状态
    /// </summary>
    public ConnectionState ConnectionState { get; set; } = null!;

    /// <summary>
    /// 设备状态
    /// </summary>
    public EquipmentState EquipmentState { get; set; }

    /// <summary>
    /// 健康状态
    /// </summary>
    public HealthStatus HealthStatus { get; set; }

    /// <summary>
    /// 活动报警数量
    /// </summary>
    public int ActiveAlarmCount { get; set; }

    /// <summary>
    /// 最后接收数据时间
    /// </summary>
    public DateTime? LastDataReceived { get; set; }

    /// <summary>
    /// 最后心跳时间
    /// </summary>
    public DateTime? LastHeartbeat { get; set; }

    /// <summary>
    /// 运行时间
    /// </summary>
    public TimeSpan? Uptime { get; set; }

    /// <summary>
    /// 诊断指标
    /// </summary>
    public IDictionary<string, object> Metrics { get; set; } = new Dictionary<string, object>();

    /// <summary>
    /// 诊断时间
    /// </summary>
    public DateTime DiagnosticTime { get; set; }

    /// <summary>
    /// 私有构造函数
    /// </summary>
    public DeviceDiagnosticInfo() { }

    /// <summary>
    /// 创建诊断信息
    /// </summary>
    /// <param name="equipmentId">设备标识</param>
    /// <param name="serviceStatus">服务状态</param>
    /// <param name="connectionState">连接状态</param>
    /// <param name="equipmentState">设备状态</param>
    /// <param name="healthStatus">健康状态</param>
    /// <returns>诊断信息</returns>
    public static DeviceDiagnosticInfo Create(
        EquipmentId equipmentId,
        DeviceServiceStatus serviceStatus,
        ConnectionState connectionState,
        EquipmentState equipmentState,
        HealthStatus healthStatus)
    {
        return new DeviceDiagnosticInfo
        {
            EquipmentId = equipmentId,
            ServiceStatus = serviceStatus,
            ConnectionState = connectionState,
            EquipmentState = equipmentState,
            HealthStatus = healthStatus,
            DiagnosticTime = DateTime.UtcNow
        };
    }

    /// <summary>
    /// 是否在线
    /// </summary>
    public bool IsOnline => ConnectionState.IsConnected && EquipmentState.IsAvailable();

    /// <summary>
    /// 是否需要关注
    /// </summary>
    public bool RequiresAttention => HealthStatus.RequiresAttention() || EquipmentState.RequiresAttention();

    /// <summary>
    /// 添加指标
    /// </summary>
    /// <param name="key">指标键</param>
    /// <param name="value">指标值</param>
    public void AddMetric(string key, object value)
    {
        Metrics[key] = value;
    }

    /// <summary>
    /// 获取指标
    /// </summary>
    /// <typeparam name="T">指标类型</typeparam>
    /// <param name="key">指标键</param>
    /// <returns>指标值</returns>
    public T? GetMetric<T>(string key)
    {
        if (Metrics.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return default;
    }
}
