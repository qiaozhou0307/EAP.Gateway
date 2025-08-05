using EAP.Gateway.Core.Common;

namespace EAP.Gateway.Core.ValueObjects;

/// <summary>
/// 连接统计信息值对象
/// </summary>
public class ConnectionStatistics : ValueObject
{
    /// <summary>
    /// 总设备数
    /// </summary>
    public int TotalDevices { get; }

    /// <summary>
    /// 已连接设备数
    /// </summary>
    public int ConnectedDevices { get; }

    /// <summary>
    /// 在线设备数
    /// </summary>
    public int OnlineDevices { get; }

    /// <summary>
    /// 连接率 (%)
    /// </summary>
    public double ConnectionRate { get; }

    /// <summary>
    /// 成功率 (%)
    /// </summary>
    public double SuccessRate { get; }

    /// <summary>
    /// 运行时间
    /// </summary>
    public TimeSpan Uptime { get; }

    /// <summary>
    /// 统计时间
    /// </summary>
    public DateTime StatisticsTime { get; }

    public ConnectionStatistics(
        int totalDevices,
        int connectedDevices,
        int onlineDevices,
        double connectionRate,
        double successRate,
        TimeSpan uptime,
        DateTime statisticsTime)
    {
        TotalDevices = Math.Max(0, totalDevices);
        ConnectedDevices = Math.Max(0, connectedDevices);
        OnlineDevices = Math.Max(0, onlineDevices);
        ConnectionRate = Math.Max(0, Math.Min(100, connectionRate));
        SuccessRate = Math.Max(0, Math.Min(100, successRate));
        Uptime = uptime;
        StatisticsTime = statisticsTime;
    }

    /// <summary>
    /// 获取健康评级
    /// </summary>
    public string GetHealthGrade()
    {
        return ConnectionRate switch
        {
            >= 95.0 => "优秀",
            >= 85.0 => "良好",
            >= 70.0 => "一般",
            >= 50.0 => "较差",
            _ => "故障"
        };
    }

    /// <summary>
    /// 是否需要关注
    /// </summary>
    public bool RequiresAttention => ConnectionRate < 80.0 || (TotalDevices > 0 && OnlineDevices == 0);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return TotalDevices;
        yield return ConnectedDevices;
        yield return OnlineDevices;
        yield return ConnectionRate;
        yield return SuccessRate;
        yield return Uptime;
        yield return StatisticsTime;
    }
}
