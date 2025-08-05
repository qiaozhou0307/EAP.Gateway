using System.Data;
using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using EAP.Gateway.Core.Common;

namespace EAP.Gateway.Core.ValueObjects;

/// <summary>
/// 设备状态值对象
/// </summary>
public class DeviceStatus : ValueObject
{
    public EquipmentId EquipmentId { get; private set; }
    public HealthStatus HealthStatus { get; private set; }
    public ConnectionStatus ConnectionStatus { get; private set; }
    public DateTime LastUpdate { get; private set; }
    public string? StatusMessage { get; private set; }

    private DeviceStatus() { }

    public DeviceStatus(EquipmentId equipmentId, HealthStatus healthStatus, ConnectionStatus connectionStatus, string? statusMessage = null)
    {
        EquipmentId = equipmentId;
        HealthStatus = healthStatus;
        ConnectionStatus = connectionStatus;
        StatusMessage = statusMessage;
        LastUpdate = DateTime.UtcNow;
    }

    public bool IsHealthy => HealthStatus == HealthStatus.Healthy;
    public bool IsConnected => ConnectionStatus == ConnectionStatus.Connected;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return EquipmentId;
        yield return HealthStatus;
        yield return ConnectionStatus;
        yield return LastUpdate;
        yield return StatusMessage ?? string.Empty;
    }
}
