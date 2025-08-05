using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using EAP.Gateway.Core.ValueObjects;

namespace EAP.Gateway.Core.Models;



/// <summary>
/// 多连接结果汇总
/// </summary>
public class MultiConnectionResult
{
    public IReadOnlyList<DicingMachineConnectionResult> Results { get; }
    public int TotalCount => Results.Count;
    public int SuccessfulCount => Results.Count(r => r.IsSuccessful);
    public int FailedCount => Results.Count(r => !r.IsSuccessful);
    public double SuccessRate => TotalCount > 0 ? (double)SuccessfulCount / TotalCount * 100 : 0;
    public DateTime StartTime { get; }
    public TimeSpan TotalDuration { get; }
    public DateTime CompletedAt { get; }

    public MultiConnectionResult(
        IEnumerable<DicingMachineConnectionResult> results,
        DateTime startTime,
        TimeSpan totalDuration)
    {
        Results = results.ToList();
        StartTime = startTime;
        TotalDuration = totalDuration;
        CompletedAt = startTime.Add(totalDuration);
    }

    /// <summary>
    /// 获取成功连接的设备
    /// </summary>
    public IEnumerable<DicingMachineConnectionResult> GetSuccessfulConnections() =>
        Results.Where(r => r.IsSuccessful);

    /// <summary>
    /// 获取失败连接的设备
    /// </summary>
    public IEnumerable<DicingMachineConnectionResult> GetFailedConnections() =>
        Results.Where(r => !r.IsSuccessful);
}

/// <summary>
/// 设备信息获取结果
/// </summary>
public class DeviceInfoResult
{
    public bool IsSuccessful { get; }
    public Dictionary<string, string> DeviceIdentification { get; }
    public string? ErrorMessage { get; }

    private DeviceInfoResult(bool isSuccessful, Dictionary<string, string> deviceIdentification, string? errorMessage = null)
    {
        IsSuccessful = isSuccessful;
        DeviceIdentification = deviceIdentification;
        ErrorMessage = errorMessage;
    }

    public static DeviceInfoResult Successful(Dictionary<string, string> deviceIdentification) =>
        new(true, deviceIdentification);

    public static DeviceInfoResult Failed(string errorMessage) =>
        new(false, new Dictionary<string, string>(), errorMessage);
}
