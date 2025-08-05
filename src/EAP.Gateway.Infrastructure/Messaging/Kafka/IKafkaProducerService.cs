namespace EAP.Gateway.Infrastructure.Messaging.Kafka;

/// <summary>
/// Kafka生产者服务接口
/// </summary>
public interface IKafkaProducerService : IDisposable
{
    Task ProduceAsync<T>(string topic, T message, CancellationToken cancellationToken = default);
    Task ProduceAsync<T>(string topic, string key, T message, CancellationToken cancellationToken = default);
    Task FlushAsync(CancellationToken cancellationToken = default);
}
