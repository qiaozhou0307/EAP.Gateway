using EAP.Gateway.Core.Common;

namespace EAP.Gateway.Core.ValueObjects;

/// <summary>
/// 报警配置
/// </summary>
public class AlarmConfiguration : ValueObject
{
    /// <summary>
    /// 是否启用报警自动确认
    /// </summary>
    public bool EnableAutoAcknowledge { get; }

    /// <summary>
    /// 自动确认延迟（秒）
    /// </summary>
    public int AutoAcknowledgeDelay { get; }

    /// <summary>
    /// 报警严重程度过滤级别
    /// </summary>
    public AlarmSeverity MinimumSeverity { get; }

    /// <summary>
    /// 最大活动报警数量
    /// </summary>
    public int MaxActiveAlarms { get; }

    /// <summary>
    /// 报警历史保留天数
    /// </summary>
    public int HistoryRetentionDays { get; }

    /// <summary>
    /// 是否启用报警分组
    /// </summary>
    public bool EnableAlarmGrouping { get; }

    public AlarmConfiguration(
        bool enableAutoAcknowledge = false,
        int autoAcknowledgeDelay = 300,
        AlarmSeverity minimumSeverity = AlarmSeverity.INFO,
        int maxActiveAlarms = 1000,
        int historyRetentionDays = 30,
        bool enableAlarmGrouping = true)
    {
        EnableAutoAcknowledge = enableAutoAcknowledge;
        AutoAcknowledgeDelay = autoAcknowledgeDelay > 0 ? autoAcknowledgeDelay : throw new ArgumentException("Auto acknowledge delay must be positive", nameof(autoAcknowledgeDelay));
        MinimumSeverity = minimumSeverity;
        MaxActiveAlarms = maxActiveAlarms > 0 ? maxActiveAlarms : throw new ArgumentException("Max active alarms must be positive", nameof(maxActiveAlarms));
        HistoryRetentionDays = historyRetentionDays > 0 ? historyRetentionDays : throw new ArgumentException("History retention days must be positive", nameof(historyRetentionDays));
        EnableAlarmGrouping = enableAlarmGrouping;
    }

    /// <summary>
    /// 创建默认报警配置
    /// </summary>
    public static AlarmConfiguration Default() => new();

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return EnableAutoAcknowledge;
        yield return AutoAcknowledgeDelay;
        yield return MinimumSeverity;
        yield return MaxActiveAlarms;
        yield return HistoryRetentionDays;
        yield return EnableAlarmGrouping;
    }
}
