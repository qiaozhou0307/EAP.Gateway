using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using EAP.Gateway.Core.ValueObjects;

namespace EAP.Gateway.Core.Models;

/// <summary>
/// 裂片机连接结果
/// </summary>
public class DicingMachineConnectionResult
{
    public bool IsSuccessful { get; }
    public EquipmentId? EquipmentId { get; }
    public string IpAddress { get; }
    public int Port { get; }
    public DicingMachineMetadata? MachineMetadata { get; }
    public string? ErrorMessage { get; }
    public DateTime StartTime { get; }
    public TimeSpan Duration { get; }
    public DateTime CompletedAt { get; }

    private DicingMachineConnectionResult(
        bool isSuccessful,
        string ipAddress,
        int port,
        DateTime startTime,
        TimeSpan duration,
        EquipmentId? equipmentId = null,
        DicingMachineMetadata? metadata = null,
        string? errorMessage = null)
    {
        IsSuccessful = isSuccessful;
        IpAddress = ipAddress;
        Port = port;
        StartTime = startTime;
        Duration = duration;
        CompletedAt = startTime.Add(duration);
        EquipmentId = equipmentId;
        MachineMetadata = metadata;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// 创建成功的连接结果
    /// </summary>
    public static DicingMachineConnectionResult Successful(
        EquipmentId equipmentId,
        DicingMachineMetadata metadata,
        DateTime startTime,
        TimeSpan duration)
    {
        return new DicingMachineConnectionResult(
            isSuccessful: true,
            ipAddress: metadata.ExtendedProperties.GetValueOrDefault("IP_ADDRESS", "Unknown"),
            port: int.TryParse(metadata.ExtendedProperties.GetValueOrDefault("PORT", "0"), out var p) ? p : 0,
            startTime: startTime,
            duration: duration,
            equipmentId: equipmentId,
            metadata: metadata);
    }

    /// <summary>
    /// 创建失败的连接结果
    /// </summary>
    public static DicingMachineConnectionResult Failed(
        string ipAddress,
        int port,
        string errorMessage,
        DateTime startTime)
    {
        return new DicingMachineConnectionResult(
            isSuccessful: false,
            ipAddress: ipAddress,
            port: port,
            startTime: startTime,
            duration: DateTime.UtcNow - startTime,
            errorMessage: errorMessage);
    }
}

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
