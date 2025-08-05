using System.ComponentModel.DataAnnotations;

namespace EAP.Gateway.Infrastructure.Configuration;

/// <summary>
/// Kafka配置类
/// </summary>
public class KafkaConfig
{
    /// <summary>
    /// Kafka集群地址
    /// </summary>
    [Required]
    public string BootstrapServers { get; set; } = "localhost:9092";

    /// <summary>
    /// 追踪数据主题
    /// </summary>
    public string TraceDataTopic { get; set; } = "eap.trace_data";

    /// <summary>
    /// 设备事件主题
    /// </summary>
    public string EquipmentEventsTopic { get; set; } = "eap.equipment_events";

    /// <summary>
    /// 报警事件主题
    /// </summary>
    public string AlarmEventsTopic { get; set; } = "eap.alarm_events";

    /// <summary>
    /// 设备状态变更主题
    /// </summary>
    public string DeviceStatusTopic { get; set; } = "eap.device_status";

    /// <summary>
    /// 数据变量主题
    /// </summary>
    public string DataVariablesTopic { get; set; } = "eap.data_variables";

    /// <summary>
    /// 远程命令结果主题
    /// </summary>
    public string CommandResultsTopic { get; set; } = "eap.command_results";

    /// <summary>
    /// 生产者超时时间（毫秒）
    /// </summary>
    public int ProducerTimeoutMs { get; set; } = 30000;

    /// <summary>
    /// 最大重试次数
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// 批量发送大小
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// 启用压缩
    /// </summary>
    public bool EnableCompression { get; set; } = true;

    /// <summary>
    /// 压缩类型
    /// </summary>
    public string CompressionType { get; set; } = "lz4";

    /// <summary>
    /// 启用幂等性
    /// </summary>
    public bool EnableIdempotence { get; set; } = true;

    /// <summary>
    /// 确认模式
    /// </summary>
    public string Acks { get; set; } = "all";
}
