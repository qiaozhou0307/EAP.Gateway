using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using EAP.Gateway.Core.Repositories; // ✅ 只引用Core层接口
using EAP.Gateway.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace EAP.Gateway.Infrastructure.Messaging.RabbitMQ;

/// <summary>
/// RabbitMQ服务实现
/// 实现Core.Repositories.IRabbitMQService接口
/// </summary>
public class RabbitMQService : IRabbitMQService
{
    private readonly RabbitMQConfig _config;
    private readonly ILogger<RabbitMQService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    private IConnection? _connection;
    private IModel? _channel;
    private volatile bool _disposed = false;

    public bool IsConnected => _connection?.IsOpen == true && _channel?.IsOpen == true;

    public RabbitMQService(IOptions<RabbitMQConfig> config, ILogger<RabbitMQService> logger)
    {
        _config = config.Value ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        InitializeConnection();
    }

    private void InitializeConnection()
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _config.HostName,
                Port = _config.Port,
                UserName = _config.UserName,
                Password = _config.Password,
                VirtualHost = _config.VirtualHost,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _logger.LogInformation("RabbitMQ连接已建立");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "建立RabbitMQ连接失败");
            throw;
        }
    }

    public async Task<bool> PublishAsync<T>(string exchange, string routingKey, T message, CancellationToken cancellationToken = default) where T : class
    {
        ThrowIfDisposed();

        try
        {
            var serializedMessage = JsonSerializer.Serialize(message, _jsonOptions);
            var body = Encoding.UTF8.GetBytes(serializedMessage);

            _channel?.BasicPublish(exchange, routingKey, null, body);

            _logger.LogDebug("RabbitMQ消息发布成功 [Exchange: {Exchange}, RoutingKey: {RoutingKey}]", exchange, routingKey);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RabbitMQ消息发布失败 [Exchange: {Exchange}, RoutingKey: {RoutingKey}]", exchange, routingKey);
            return false;
        }
    }

    public async Task<bool> SendCommandAsync<T>(T command, CancellationToken cancellationToken = default) where T : class
    {
        return await PublishAsync(_config.Exchanges.Commands, "command", command, cancellationToken);
    }

    public async Task<bool> PublishEquipmentEventAsync(string equipmentId, string eventType, object eventData, CancellationToken cancellationToken = default)
    {
        var eventMessage = new
        {
            EquipmentId = equipmentId,
            EventType = eventType,
            EventData = eventData,
            Timestamp = DateTime.UtcNow
        };

        return await PublishAsync(_config.Exchanges.Events, $"equipment.{equipmentId}", eventMessage, cancellationToken);
    }

    public async Task<bool> SendCommandResponseAsync<T>(string commandId, T response, CancellationToken cancellationToken = default) where T : class
    {
        var responseMessage = new
        {
            CommandId = commandId,
            Response = response,
            Timestamp = DateTime.UtcNow
        };

        return await PublishAsync(_config.Exchanges.Commands, "command.response", responseMessage, cancellationToken);
    }

    public Task StartConsumingAsync(CancellationToken cancellationToken = default)
    {
        // 实现消息消费逻辑
        _logger.LogInformation("开始RabbitMQ消息消费");
        return Task.CompletedTask;
    }

    public Task StopConsumingAsync(CancellationToken cancellationToken = default)
    {
        // 实现停止消费逻辑
        _logger.LogInformation("停止RabbitMQ消息消费");
        return Task.CompletedTask;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(RabbitMQService));
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _channel?.Close();
            _channel?.Dispose();
            _connection?.Close();
            _connection?.Dispose();
            _disposed = true;
            _logger.LogInformation("RabbitMQ服务已释放");
        }
    }

    public async ValueTask DisposeAsync()
    {
        Dispose();
        await Task.CompletedTask;
    }
}
