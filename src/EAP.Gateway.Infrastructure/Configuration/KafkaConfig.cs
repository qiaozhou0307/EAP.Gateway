namespace EAP.Gateway.Infrastructure.Configuration;

/// <summary>
/// Kafka配置
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
}
