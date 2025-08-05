using Confluent.Kafka;
using EAP.Gateway.Infrastructure.Configuration;

namespace EAP.Gateway.Infrastructure.Extensions;

/// <summary>
/// Kafka配置扩展方法
/// </summary>
public static class KafkaConfigExtensions
{
    /// <summary>
    /// 验证 Kafka 配置
    /// </summary>
    /// <param name="kafkaConfig">Kafka配置</param>
    /// <returns>验证结果</returns>
    public static (bool IsValid, string[] Errors) Validate(this KafkaConfig kafkaConfig)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(kafkaConfig.BootstrapServers))
            errors.Add("BootstrapServers 不能为空");

        if (kafkaConfig.ProducerTimeoutMs <= 0)
            errors.Add("ProducerTimeoutMs 必须大于0");

        if (kafkaConfig.MaxRetries < 0)
            errors.Add("MaxRetries 不能小于0");

        if (kafkaConfig.BatchSize <= 0)
            errors.Add("BatchSize 必须大于0");

        if (!Enum.TryParse<Acks>(kafkaConfig.Acks, true, out _))
            errors.Add($"无效的 Acks 值: {kafkaConfig.Acks}");

        if (!Enum.TryParse<CompressionType>(kafkaConfig.CompressionType, true, out _))
            errors.Add($"无效的 CompressionType 值: {kafkaConfig.CompressionType}");

        return (errors.Count == 0, errors.ToArray());
    }

    /// <summary>
    /// 创建生产者配置
    /// </summary>
    /// <param name="kafkaConfig">Kafka配置</param>
    /// <returns>生产者配置</returns>
    public static ProducerConfig CreateProducerConfig(this KafkaConfig kafkaConfig)
    {
        return new ProducerConfig
        {
            BootstrapServers = kafkaConfig.BootstrapServers,
            Acks = Enum.Parse<Acks>(kafkaConfig.Acks, true),
            MessageTimeoutMs = kafkaConfig.ProducerTimeoutMs,
            EnableIdempotence = kafkaConfig.EnableIdempotence,
            MessageSendMaxRetries = kafkaConfig.MaxRetries,
            RetryBackoffMs = 1000,
            BatchSize = kafkaConfig.BatchSize,
            LingerMs = 5,
            CompressionType = Enum.Parse<CompressionType>(kafkaConfig.CompressionType, true)
        };
    }
}
