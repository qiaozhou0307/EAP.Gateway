using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using EAP.Gateway.Core.ValueObjects;

namespace EAP.Gateway.Core.Models;

/// <summary>
/// 裂片机状态信息
/// </summary>
public class DicingMachineStatus
{
    public EquipmentId EquipmentId { get; set; } = default!;
    public string MachineNumber { get; set; } = string.Empty;
    public string MachineVersion { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public bool IsConnected { get; set; }
    public bool IsOnline { get; set; }
    public HealthStatus HealthStatus { get; set; }
    public EquipmentState ConnectionState { get; set; }
    public DateTime? LastHeartbeat { get; set; }
    public DateTime RegisteredAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// 获取状态描述
    /// </summary>
    public string GetStatusDescription()
    {
        if (!IsConnected)
            return "离线";

        if (!IsOnline)
            return "已连接但不可用";

        return HealthStatus switch
        {
            HealthStatus.Healthy => "正常运行",
            HealthStatus.Degraded => "性能下降",
            HealthStatus.Unhealthy => "故障状态",
            _ => "状态未知"
        };
    }
}
