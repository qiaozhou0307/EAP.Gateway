using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using EAP.Gateway.Core.Repositories; // ✅ 只引用Core层接口
using EAP.Gateway.Infrastructure.Configuration;

namespace EAP.Gateway.Infrastructure.Messaging.Kafka;

/// <summary>
/// Kafka消息发布服务实现（修复返回类型版本）
/// 实现Core.Repositories.IKafkaProducerService接口
/// </summary>
public class KafkaProducerService : IKafkaProducerService
{
    private readonly IProducer<string, string> _producer;
    private readonly KafkaConfig _config;
    private readonly ILogger<KafkaProducerService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private volatile bool _disposed = false;

    public bool IsConnected => !_disposed && _producer != null;

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

        var producerConfig = new ProducerConfig
        {
            BootstrapServers = _config.BootstrapServers,
            EnableIdempotence = _config.ProducerConfig.EnableIdempotence,
            MessageTimeoutMs = _config.ProducerConfig.MessageTimeoutMs,
            RequestTimeoutMs = _config.ProducerConfig.RequestTimeoutMs,
            RetryBackoffMs = _config.ProducerConfig.RetryBackoffMs,
            BatchSize = _config.ProducerConfig.BatchSize,
            LingerMs = _config.ProducerConfig.LingerMs,
            CompressionType = Enum.Parse<CompressionType>(_config.ProducerConfig.CompressionType, true),
            DeliveryReportFields = "key,value,timestamp,headers"
        };

        _producer = new ProducerBuilder<string, string>(producerConfig)
            .SetErrorHandler((_, e) =>
                _logger.LogError("Kafka Producer错误: {Error} - {Reason}", e.Code, e.Reason))
            .SetLogHandler((_, logMessage) =>
                _logger.LogDebug("Kafka Producer日志: {Level} - {Message}", logMessage.Level, logMessage.Message))
            .Build();

        _logger.LogInformation("Kafka Producer服务已初始化");
    }

    #region Core层业务方法实现

    public async Task<bool> PublishTraceDataAsync<T>(string topic, string key, T data, CancellationToken cancellationToken = default) where T : class
    {
        return await ProduceAsync(topic, key, data, cancellationToken);
    }

    public async Task<bool> PublishEquipmentEventAsync(string equipmentId, string eventType, object eventData, CancellationToken cancellationToken = default)
    {
        try
        {
            var topic = _config.Topics.EquipmentData;
            var key = $"{equipmentId}:{eventType}";

            var eventMessage = new
            {
                EquipmentId = equipmentId,
                EventType = eventType,
                EventData = eventData,
                Timestamp = DateTime.UtcNow
            };

            return await ProduceAsync(topic, key, eventMessage, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发布设备事件失败 [设备: {EquipmentId}, 事件: {EventType}]", equipmentId, eventType);
            return false;
        }
    }

    public async Task<bool> PublishAlarmEventAsync(string equipmentId, string alarmType, object alarmData, CancellationToken cancellationToken = default)
    {
        try
        {
            var topic = _config.Topics.AlarmEvents;
            var key = $"{equipmentId}:{alarmType}";

            var alarmMessage = new
            {
                EquipmentId = equipmentId,
                AlarmType = alarmType,
                AlarmData = alarmData,
                Timestamp = DateTime.UtcNow
            };

            return await ProduceAsync(topic, key, alarmMessage, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发布报警事件失败 [设备: {EquipmentId}, 报警: {AlarmType}]", equipmentId, alarmType);
            return false;
        }
    }

    public async Task<bool> PublishDeviceStatusAsync(string equipmentId, object statusData, CancellationToken cancellationToken = default)
    {
        try
        {
            var topic = _config.Topics.EquipmentData;
            var key = $"{equipmentId}:status";

            var statusMessage = new
            {
                EquipmentId = equipmentId,
                StatusData = statusData,
                Timestamp = DateTime.UtcNow
            };

            return await ProduceAsync(topic, key, statusMessage, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发布设备状态失败 [设备: {EquipmentId}]", equipmentId);
            return false;
        }
    }

    #endregion

    #region Infrastructure层基础方法实现

    public async Task<bool> ProduceAsync<T>(string topic, T message, CancellationToken cancellationToken = default) where T : class
    {
        return await ProduceAsync(topic, null, message, cancellationToken);
    }

    public async Task<bool> ProduceAsync<T>(string topic, string? key, T message, CancellationToken cancellationToken = default) where T : class
    {
        ThrowIfDisposed();

        if (string.IsNullOrWhiteSpace(topic))
        {
            _logger.LogError("Topic不能为空");
            return false;
        }

        if (message == null)
        {
            _logger.LogError("消息不能为null");
            return false;
        }

        try
        {
            var serializedMessage = JsonSerializer.Serialize(message, _jsonOptions);
            var kafkaMessage = new Message<string, string>
            {
                Key = key ?? string.Empty, // 修复CS8601: 确保Key不会为null
                Value = serializedMessage,
                Timestamp = new Timestamp(DateTime.UtcNow)
            };

            var deliveryResult = await _producer.ProduceAsync(topic, kafkaMessage, cancellationToken);

            _logger.LogDebug("消息发送成功 [Topic: {Topic}, Key: {Key}, Partition: {Partition}, Offset: {Offset}]",
                topic, key, deliveryResult.Partition.Value, deliveryResult.Offset.Value);

            return true; // ✅ 成功时返回true
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex, "Kafka消息发送失败 [Topic: {Topic}, Key: {Key}, Error: {Error}]",
                topic, key, ex.Error.Reason);
            return false; // ✅ 失败时返回false
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Kafka消息发送被取消 [Topic: {Topic}, Key: {Key}]", topic, key);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kafka消息发送异常 [Topic: {Topic}, Key: {Key}]", topic, key);
            return false;
        }
    }

    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            // Confluent.Kafka的Flush方法是同步的，但我们包装成异步
            await Task.Run(() => _producer.Flush(cancellationToken), cancellationToken);
            _logger.LogDebug("Kafka Producer刷新完成");
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Kafka Producer刷新被取消");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kafka Producer刷新失败");
            throw;
        }
    }

    #endregion

    #region 资源释放

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(KafkaProducerService));
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            try
            {
                // 在释放前刷新所有待发送的消息
                _producer?.Flush(TimeSpan.FromSeconds(5));
                _producer?.Dispose();
                _logger.LogInformation("Kafka Producer服务已释放");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "释放Kafka Producer时发生异常");
            }
            finally
            {
                _disposed = true;
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            try
            {
                // 异步刷新
                await FlushAsync(CancellationToken.None);
                _producer?.Dispose();
                _logger.LogInformation("Kafka Producer服务已异步释放");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "异步释放Kafka Producer时发生异常");
            }
            finally
            {
                _disposed = true;
            }
        }
    }

    #endregion
}
