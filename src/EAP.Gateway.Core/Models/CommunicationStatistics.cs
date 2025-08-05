using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using EAP.Gateway.Core.ValueObjects;

namespace EAP.Gateway.Core.Models;

/// <summary>
/// 通信统计信息
/// </summary>
public class CommunicationStatistics
{
    /// <summary>
    /// 设备标识
    /// </summary>
    public EquipmentId EquipmentId { get; set; } = null!;

    /// <summary>
    /// 连接统计
    /// </summary>
    public ConnectionStatistics ConnectionStats { get; set; } = null!;

    /// <summary>
    /// 统计时间
    /// </summary>
    public DateTime StatisticsTime { get; set; }

    /// <summary>
    /// 私有构造函数
    /// </summary>
    private CommunicationStatistics() { }

    /// <summary>
    /// 创建通信统计信息
    /// </summary>
    /// <param name="equipmentId">设备标识</param>
    /// <param name="connectionStats">连接统计</param>
    /// <returns>通信统计信息</returns>
    public static CommunicationStatistics Create(EquipmentId equipmentId, ConnectionStatistics connectionStats)
    {
        return new CommunicationStatistics
        {
            EquipmentId = equipmentId,
            ConnectionStats = connectionStats,
            StatisticsTime = DateTime.UtcNow
        };
    }
}
