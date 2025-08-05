// src/EAP.Gateway.Infrastructure/Configuration/KafkaConfig.cs (修复版本)
using Confluent.Kafka;

namespace EAP.Gateway.Infrastructure.Configuration;

/// <summary>
/// Kafka配置（修复版本）
/// </summary>
public class KafkaConfig
{
    public const string SectionName = "Kafka";

    public string BootstrapServers { get; set; } = string.Empty;
    public string ClientId { get; set; } = "EAP.Gateway";
    public bool EnableAutoOffsetStore { get; set; } = true;
    public string SecurityProtocol { get; set; } = "PlainText";
    public KafkaTopics Topics { get; set; } = new();
    public KafkaProducerConfig ProducerConfig { get; set; } = new();

    public (bool IsValid, IEnumerable<string> Errors) Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(BootstrapServers))
            errors.Add("BootstrapServers is required");

        if (string.IsNullOrWhiteSpace(ClientId))
            errors.Add("ClientId is required");

        // 验证Topics
        var (topicsValid, topicsErrors) = Topics.Validate();
        if (!topicsValid)
            errors.AddRange(topicsErrors);

        // 验证ProducerConfig
        var (producerValid, producerErrors) = ProducerConfig.Validate();
        if (!producerValid)
            errors.AddRange(producerErrors);

        return (errors.Count == 0, errors);
    }
}

public class KafkaTopics
{
    public string EquipmentData { get; set; } = "eap.equipment.data";
    public string AlarmEvents { get; set; } = "eap.alarm.events";
    public string CheckData { get; set; } = "eap.check.data";
    public string GoldenSampleData { get; set; } = "eap.golden.sample.data";
    public string YieldData { get; set; } = "eap.yield.data";

    public (bool IsValid, IEnumerable<string> Errors) Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(EquipmentData))
            errors.Add("EquipmentData topic is required");

        if (string.IsNullOrWhiteSpace(AlarmEvents))
            errors.Add("AlarmEvents topic is required");

        return (errors.Count == 0, errors);
    }
}

public class KafkaProducerConfig
{
    public bool EnableIdempotence { get; set; } = true;
    public int RetryBackoffMs { get; set; } = 100;
    public int MessageTimeoutMs { get; set; } = 30000;
    public int RequestTimeoutMs { get; set; } = 30000;
    public int BatchSize { get; set; } = 16384;
    public int LingerMs { get; set; } = 5;
    public string CompressionType { get; set; } = "Gzip";

    // ✅ 新增属性，确保与Confluent.Kafka兼容
    public string Acks { get; set; } = "All"; // None, Leader, All
    public int MaxRetries { get; set; } = 3;
    public int ProducerTimeoutMs { get; set; } = 30000;

    public (bool IsValid, IEnumerable<string> Errors) Validate()
    {
        var errors = new List<string>();

        if (MessageTimeoutMs <= 0)
            errors.Add("MessageTimeoutMs must be greater than 0");

        if (RequestTimeoutMs <= 0)
            errors.Add("RequestTimeoutMs must be greater than 0");

        if (BatchSize <= 0)
            errors.Add("BatchSize must be greater than 0");

        var validCompressionTypes = new[] { "None", "Gzip", "Snappy", "Lz4", "Zstd" };
        if (!validCompressionTypes.Contains(CompressionType, StringComparer.OrdinalIgnoreCase))
            errors.Add($"CompressionType must be one of: {string.Join(", ", validCompressionTypes)}");

        var validAcks = new[] { "None", "Leader", "All" };
        if (!validAcks.Contains(Acks, StringComparer.OrdinalIgnoreCase))
            errors.Add($"Acks must be one of: {string.Join(", ", validAcks)}");

        return (errors.Count == 0, errors);
    }
}
