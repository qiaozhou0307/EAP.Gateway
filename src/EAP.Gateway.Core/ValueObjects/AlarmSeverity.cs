namespace EAP.Gateway.Core.ValueObjects;

/// <summary>
/// 报警严重程度枚举，符合SEMI E30标准
/// </summary>
public enum AlarmSeverity
{
    /// <summary>
    /// 信息 - 仅作记录，不需要操作员干预
    /// </summary>
    INFO = 0,

    /// <summary>
    /// 轻微 - 需要操作员注意但不影响正常操作
    /// </summary>
    MINOR = 1,

    /// <summary>
    /// 一般 - 需要操作员关注并可能需要采取行动
    /// </summary>
    MAJOR = 2,

    /// <summary>
    /// 严重 - 需要立即操作员干预
    /// </summary>
    CRITICAL = 3,

    /// <summary>
    /// 紧急 - 系统或安全相关的严重问题
    /// </summary>
    EMERGENCY = 4
}


/// <summary>
/// 报警严重程度扩展方法
/// </summary>
public static class AlarmSeverityExtensions
{
    /// <summary>
    /// 检查是否为严重报警
    /// </summary>
    public static bool IsCritical(this AlarmSeverity severity)
    {
        return severity >= AlarmSeverity.MAJOR;
    }

    /// <summary>
    /// 检查是否需要立即响应
    /// </summary>
    public static bool RequiresImmediateResponse(this AlarmSeverity severity)
    {
        return severity == AlarmSeverity.CRITICAL;
    }

    /// <summary>
    /// 获取严重程度的显示名称
    /// </summary>
    public static string GetDisplayName(this AlarmSeverity severity)
    {
        return severity switch
        {
            AlarmSeverity.INFO => "Information",
            AlarmSeverity.MINOR => "Minor", 
            AlarmSeverity.MAJOR => "Major",
            AlarmSeverity.CRITICAL => "Critical",
            AlarmSeverity.EMERGENCY => "Emergency",
            _ => severity.ToString()
        };
    }

    /// <summary>
    /// 获取严重程度的颜色代码（用于UI显示）
    /// </summary>
    public static string GetColorCode(this AlarmSeverity severity)
    {
        return severity switch
        {
            AlarmSeverity.INFO => "#2196F3",      // Blue
            AlarmSeverity.MINOR => "#FFC107",     // Amber
            AlarmSeverity.MAJOR => "#FF9800",     // Deep Orange
            AlarmSeverity.CRITICAL => "#F44336",  // Red
            AlarmSeverity.EMERGENCY => "#FF5722",   // Orange
            _ => "#9E9E9E"                         // Grey
        };
    }

    /// <summary>
    /// 获取严重程度的优先级（数值越大优先级越高）
    /// </summary>
    public static int GetPriority(this AlarmSeverity severity)
    {
        return (int)severity;
    }
}
