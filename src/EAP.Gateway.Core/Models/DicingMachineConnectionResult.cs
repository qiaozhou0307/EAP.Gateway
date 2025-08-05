using EAP.Gateway.Core.Aggregates.EquipmentAggregate;

using EAP.Gateway.Core.ValueObjects;

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
