namespace EAP.Gateway.Core.Repositories;

/// <summary>
/// RabbitMQ服务接口（Core层定义）
/// </summary>
public interface IRabbitMQService : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// 发布消息到指定交换机
    /// </summary>
    Task<bool> PublishAsync<T>(string exchange, string routingKey, T message, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// 发送命令消息
    /// </summary>
    Task<bool> SendCommandAsync<T>(T command, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// 开始消费消息
    /// </summary>
    Task StartConsumingAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 停止消费消息
    /// </summary>
    Task StopConsumingAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 连接状态
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// 发布设备事件
    /// </summary>
    Task<bool> PublishEquipmentEventAsync(string equipmentId, string eventType, object eventData, CancellationToken cancellationToken = default);

    /// <summary>
    /// 发送命令响应
    /// </summary>
    Task<bool> SendCommandResponseAsync<T>(string commandId, T response, CancellationToken cancellationToken = default) where T : class;
}
