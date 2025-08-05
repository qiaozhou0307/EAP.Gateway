namespace EAP.Gateway.Application.DTOs;

/// <summary>
/// 设备详细信息DTO
/// </summary>
public class EquipmentDetailsDto
{
    public string EquipmentId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string State { get; set; } = string.Empty;
    public string? SubState { get; set; }
    public bool IsConnected { get; set; }
    public string HealthStatus { get; set; } = string.Empty;
    public DateTime? LastHeartbeat { get; set; }
    public DateTime? LastDataUpdate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }

    // 可选的详细信息
    public EquipmentConfigurationDto? Configuration { get; set; }
    public ProcessingMetricsDto? Metrics { get; set; }
    public IEnumerable<AlarmEventDto>? ActiveAlarms { get; set; }
    public IEnumerable<RemoteCommandDto>? RecentCommands { get; set; }
}

/// <summary>
/// 设备配置DTO
/// </summary>
public class EquipmentConfigurationDto
{
    public string? Manufacturer { get; set; }
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
    public string? IpAddress { get; set; }
    public int? Port { get; set; }
    public bool EnableDataCollection { get; set; }
    public int? DataCollectionInterval { get; set; }
    public bool EnableAlarmCollection { get; set; }
}

/// <summary>
/// 处理指标DTO - 统一版本
/// </summary>
public class ProcessingMetricsDto
{
    public long TotalProcessedItems { get; set; }
    public TimeSpan TotalProcessingTime { get; set; }
    public double AverageProcessingTime { get; set; }
    public int ErrorCount { get; set; }
    public DateTime? LastResetTime { get; set; }

    // 新增：更完整的指标信息
    public int TotalProcessed { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public double SuccessRate { get; set; }
    public DateTime? LastResetAt { get; set; }
}

/// <summary>
/// 报警事件DTO - 完整版本
/// </summary>
public class AlarmEventDto
{
    /// <summary>
    /// 报警ID - 修复：使用uint类型匹配AlarmEvent.AlarmId
    /// </summary>
    public uint AlarmId { get; set; }

    /// <summary>
    /// 报警代码
    /// </summary>
    public string? AlarmCode { get; set; }

    /// <summary>
    /// 报警文本描述 - 修复：可空类型匹配AlarmEvent.AlarmText
    /// </summary>
    public string? AlarmText { get; set; }

    /// <summary>
    /// 报警严重程度
    /// </summary>
    public string Severity { get; set; } = string.Empty;

    /// <summary>
    /// 报警状态
    /// </summary>
    public string State { get; set; } = string.Empty;

    /// <summary>
    /// 报警设置时间
    /// </summary>
    public DateTime SetTime { get; set; }

    /// <summary>
    /// 报警清除时间
    /// </summary>
    public DateTime? ClearTime { get; set; }

    /// <summary>
    /// 是否已确认 - 基于State计算
    /// </summary>
    public bool IsAcknowledged { get; set; }

    /// <summary>
    /// 确认者
    /// </summary>
    public string? AcknowledgedBy { get; set; }

    /// <summary>
    /// 确认时间
    /// </summary>
    public DateTime? AcknowledgedAt { get; set; }

    /// <summary>
    /// 是否为活跃状态
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// 报警持续时间（毫秒）
    /// </summary>
    public double DurationMs { get; set; }
}

/// <summary>
/// 远程命令DTO
/// </summary>
public class RemoteCommandDto
{
    public Guid CommandId { get; set; }
    public string CommandName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? RequestedBy { get; set; }
    public string? ResultMessage { get; set; }
}
