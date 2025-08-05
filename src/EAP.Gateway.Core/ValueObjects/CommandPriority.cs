namespace EAP.Gateway.Core.ValueObjects;

/// <summary>
/// 命令优先级枚举
/// </summary>
public enum CommandPriority
{
    /// <summary>
    /// 低优先级
    /// </summary>
    Low = 0,

    /// <summary>
    /// 普通优先级
    /// </summary>
    Normal = 1,

    /// <summary>
    /// 高优先级
    /// </summary>
    High = 2,

    /// <summary>
    /// 紧急优先级
    /// </summary>
    Critical = 3
}

/// <summary>
/// 命令优先级扩展方法
/// </summary>
public static class CommandPriorityExtensions
{
    /// <summary>
    /// 获取优先级显示名称
    /// </summary>
    public static string GetDisplayName(this CommandPriority priority)
    {
        return priority switch
        {
            CommandPriority.Low => "低",
            CommandPriority.Normal => "普通",
            CommandPriority.High => "高",
            CommandPriority.Critical => "紧急",
            _ => priority.ToString()
        };
    }

    /// <summary>
    /// 获取优先级数值（用于排序）
    /// </summary>
    public static int GetNumericValue(this CommandPriority priority)
    {
        return (int)priority;
    }

    /// <summary>
    /// 检查是否为高优先级命令
    /// </summary>
    public static bool IsHighPriority(this CommandPriority priority)
    {
        return priority >= CommandPriority.High;
    }
}
