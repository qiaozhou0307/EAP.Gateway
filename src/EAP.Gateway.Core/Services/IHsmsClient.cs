using EAP.Gateway.Core.Aggregates.MessageAggregate;
using EAP.Gateway.Core.Events.Equipment;
using EAP.Gateway.Core.ValueObjects;

namespace EAP.Gateway.Core.Services;

/// <summary>
/// HSMS客户端接口
/// </summary>
public interface IHsmsClient : IDisposable
{
    bool IsConnected { get; }
    ConnectionState ConnectionState { get; }

    event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;
    event EventHandler<MessageReceivedEventArgs>? MessageReceived;
    event EventHandler<MessageTimeoutEventArgs>? MessageTimeout;

    Task<bool> ConnectAsync(CancellationToken cancellationToken = default);
    Task DisconnectAsync(CancellationToken cancellationToken = default);
    Task<SecsMessage?> SendAsync(SecsMessage message, CancellationToken cancellationToken = default);
    ConnectionStatistics GetConnectionStatistics();
}
