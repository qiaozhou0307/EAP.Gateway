namespace EAP.Gateway.Infrastructure.Messaging.RabbitMQ;

/// <summary>
/// RabbitMQ服务接口
/// </summary>
public interface IRabbitMQService : IDisposable
{
    Task PublishAsync<T>(string exchange, string routingKey, T message, CancellationToken cancellationToken = default);
    Task<bool> SendCommandAsync<T>(T command, CancellationToken cancellationToken = default);
    Task StartConsumingAsync(CancellationToken cancellationToken = default);
    Task StopConsumingAsync(CancellationToken cancellationToken = default);
}
