using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using EAP.Gateway.Core.Repositories;
using EAP.Gateway.Infrastructure.Configuration;

namespace EAP.Gateway.Infrastructure.Messaging.Kafka;

/// <summary>
/// Kafka消息发布服务实现 - 修复版本
/// </summary>
public class KafkaProducerService : IKafkaProducerService, IAsyncDisposable, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly KafkaConfig _config;
    private readonly ILogger<KafkaProducerService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private volatile bool _disposed = false;

    public KafkaProducerService(IOptions<KafkaConfig> config, ILogger<KafkaProducerService> logger)
    {
        _config = config.Value ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        // 修复：使用正确的 Confluent.Kafka ProducerConfig 属性名
        var producerConfig = new ProducerConfig
        {
            BootstrapServers = _config.BootstrapServers,
            Acks = Enum.Parse<Acks>(_config.Acks, true),
            MessageTimeoutMs = _config.ProducerTimeoutMs,
            EnableIdempotence = _config.EnableIdempotence,

            MessageSendMaxRetries = _config.MaxRetries, 
            RetryBackoffMs = 1000,

            // 性能优化配置
            BatchSize = _config.BatchSize,
            LingerMs = 5,
            CompressionType = Enum.Parse<CompressionType>(_config.CompressionType, true),

            // 错误处理
            DeliveryReportFields = "key,value,timestamp,headers"
        };

        _producer = new ProducerBuilder<string, string>(producerConfig)
            .SetErrorHandler((_, e) =>
                _logger.LogError("Kafka Producer错误: {Error} - {Reason}", e.Code, e.Reason))
            .SetLogHandler((_, logMessage) =>
                _logger.LogDebug("Kafka Producer日志: {Level} - {Message}", logMessage.Level, logMessage.Message))
            .Build();

        _logger.LogInformation("Kafka Producer服务已初始化 [Servers: {Servers}]", _config.BootstrapServers);
    }

    public async Task<bool> PublishTraceDataAsync<T>(string topic, string key, T data, CancellationToken cancellationToken = default)
        where T : class
    {
        if (_disposed)
        {
            _logger.LogWarning("Kafka Producer已释放，无法发布消息");
            return false;
        }

        try
        {
            var jsonData = JsonSerializer.Serialize(data, _jsonOptions);
            var message = new Message<string, string>
            {
                Key = key,
                Value = jsonData,
                Timestamp = new Timestamp(DateTime.UtcNow),
                Headers = new Headers
                {
                    { "content-type", System.Text.Encoding.UTF8.GetBytes("application/json") },
                    { "source", System.Text.Encoding.UTF8.GetBytes("eap-gateway") },
                    { "version", System.Text.Encoding.UTF8.GetBytes("1.0") }
                }
            };

            var deliveryResult = await _producer.ProduceAsync(topic, message, cancellationToken);

            _logger.LogDebug("Kafka消息发布成功: Topic={Topic}, Key={Key}, Partition={Partition}, Offset={Offset}, Timestamp={Timestamp}",
                topic, key, deliveryResult.Partition.Value, deliveryResult.Offset.Value, deliveryResult.Timestamp.UtcDateTime);

            return true;
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex, "Kafka消息发布失败: Topic={Topic}, Key={Key}, Error={Error}, IsFatal={IsFatal}",
                topic, key, ex.Error.Reason, ex.Error.IsFatal);
            return false;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Kafka消息发布已取消: Topic={Topic}, Key={Key}", topic, key);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kafka消息发布异常: Topic={Topic}, Key={Key}, Exception={Exception}",
                topic, key, ex.GetType().Name);
            return false;
        }
    }

    public async Task<bool> PublishEquipmentEventAsync(string equipmentId, string eventType, object eventData,
        CancellationToken cancellationToken = default)
    {
        var topic = _config.EquipmentEventsTopic;
        var key = $"{equipmentId}:{eventType}:{DateTime.UtcNow:yyyyMMddHHmmss}";

        var envelope = new
        {
            EquipmentId = equipmentId,
            EventType = eventType,
            Data = eventData,
            Timestamp = DateTime.UtcNow,
            MessageId = Guid.NewGuid().ToString(),
            Version = "1.0"
        };

        return await PublishTraceDataAsync(topic, key, envelope, cancellationToken);
    }

    public async Task<bool> PublishAlarmEventAsync(string equipmentId, string alarmType, object alarmData,
        CancellationToken cancellationToken = default)
    {
        var topic = _config.AlarmEventsTopic;
        var key = $"{equipmentId}:alarm:{alarmType}";

        var envelope = new
        {
            EquipmentId = equipmentId,
            AlarmType = alarmType,
            Data = alarmData,
            Timestamp = DateTime.UtcNow,
            MessageId = Guid.NewGuid().ToString(),
            Severity = "Warning" // 可以根据实际情况调整
        };

        return await PublishTraceDataAsync(topic, key, envelope, cancellationToken);
    }

    public async Task<bool> PublishDeviceStatusAsync(string equipmentId, object statusData,
        CancellationToken cancellationToken = default)
    {
        var topic = _config.DeviceStatusTopic;
        var key = equipmentId;

        var envelope = new
        {
            EquipmentId = equipmentId,
            Status = statusData,
            Timestamp = DateTime.UtcNow,
            MessageId = Guid.NewGuid().ToString()
        };

        return await PublishTraceDataAsync(topic, key, envelope, cancellationToken);
    }

    public void Dispose()
    {
        DisposeAsync().AsTask().Wait();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        try
        {
            _logger.LogInformation("开始释放Kafka Producer资源");

            // 刷新所有挂起的消息
            _producer?.Flush(TimeSpan.FromSeconds(10));

            // 异步释放资源
            await Task.Run(() => _producer?.Dispose());

            _logger.LogInformation("Kafka Producer资源已释放");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "释放Kafka Producer时发生异常");
        }
    }
}
