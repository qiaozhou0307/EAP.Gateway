using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using EAP.Gateway.Core.Events.Alarm;
using EAP.Gateway.Core.Events.Equipment;
using EAP.Gateway.Core.ValueObjects;

namespace EAP.Gateway.Core.Services;

/// <summary>
/// SECS设备服务接口
/// </summary>
public interface ISecsDeviceService : IAsyncDisposable
{
    EquipmentId EquipmentId { get; }
    Equipment? Equipment { get; }
    IHsmsClient HsmsClient { get; }
    bool IsStarted { get; }
    bool IsStopped { get; }
    bool IsOnline { get; }
    HealthStatus HealthStatus { get; }

    event EventHandler<DeviceServiceStatusChangedEventArgs>? StatusChanged;
    event EventHandler<DeviceDataReceivedEventArgs>? DataReceived;
    event EventHandler<DeviceAlarmEventArgs>? AlarmEvent;

    Task StartAsync(Equipment equipment, CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
    Task<bool> ConnectAsync(CancellationToken cancellationToken = default);
    Task DisconnectAsync(CancellationToken cancellationToken = default);
    Task<bool> IsConnectedAsync(CancellationToken cancellationToken = default);
    Task<Dictionary<string, string>> GetDeviceIdentificationAsync(CancellationToken cancellationToken = default);
    Task<bool> SendRemoteCommandAsync(string commandName, Dictionary<string, object>? parameters = null, CancellationToken cancellationToken = default);
}
