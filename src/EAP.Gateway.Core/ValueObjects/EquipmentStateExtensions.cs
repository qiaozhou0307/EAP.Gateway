namespace EAP.Gateway.Core.ValueObjects;

/// <summary>
/// 设备状态扩展方法
/// </summary>
public static class EquipmentStateExtensions
{
    /// <summary>
    /// 判断状态是否可用
    /// </summary>
    public static bool IsAvailable(this EquipmentState state)
    {
        return state switch
        {
            EquipmentState.IDLE => true,
            EquipmentState.EXECUTING => true,
            EquipmentState.SETUP => true,
            _ => false
        };
    }

    /// <summary>
    /// 判断状态是否需要关注
    /// </summary>
    public static bool RequiresAttention(this EquipmentState state)
    {
        return state switch
        {
            EquipmentState.FAULT => true,
            EquipmentState.ALARM => true,
            EquipmentState.MAINTENANCE => true,
            _ => false
        };
    }

    /// <summary>
    /// 获取状态显示名称
    /// </summary>
    public static string GetDisplayName(this EquipmentState state)
    {
        return state switch
        {
            EquipmentState.IDLE => "空闲",
            EquipmentState.EXECUTING => "执行中",
            EquipmentState.SETUP => "设置中",
            EquipmentState.PAUSE => "暂停",
            EquipmentState.FAULT => "故障",
            EquipmentState.ALARM => "报警",
            EquipmentState.MAINTENANCE => "维护中",
            _ => state.ToString()
        };
    }

    /// <summary>
    /// 获取状态的严重程度级别（新增方法）
    /// 用于确定状态变化事件的优先级
    /// </summary>
    /// <param name="state">设备状态</param>
    /// <returns>严重程度级别，数值越高越严重</returns>
    public static int GetSeverityLevel(this EquipmentState state)
    {
        return state switch
        {
            EquipmentState.FAULT => 5,        // 最高严重级别
            EquipmentState.ALARM => 4,        // 高严重级别
            EquipmentState.DOWN => 3,         // 中高严重级别
            EquipmentState.MAINTENANCE => 2,  // 中等严重级别
            EquipmentState.PAUSE => 2,        // 中等严重级别
            EquipmentState.SETUP => 1,        // 低严重级别
            EquipmentState.EXECUTING => 1,    // 低严重级别
            EquipmentState.IDLE => 1,         // 低严重级别
            EquipmentState.UNKNOWN => 0,      // 最低严重级别
            _ => 0                             // 默认最低级别
        };
    }

    /// <summary>
    /// 获取状态的紧急程度（补充方法）
    /// </summary>
    /// <param name="state">设备状态</param>
    /// <returns>紧急程度级别</returns>
    public static UrgencyLevel GetUrgencyLevel(this EquipmentState state)
    {
        return state switch
        {
            EquipmentState.FAULT => UrgencyLevel.Critical,
            EquipmentState.ALARM => UrgencyLevel.High,
            EquipmentState.DOWN => UrgencyLevel.Medium,
            EquipmentState.MAINTENANCE => UrgencyLevel.Low,
            EquipmentState.PAUSE => UrgencyLevel.Low,
            _ => UrgencyLevel.None
        };
    }

    /// <summary>
    /// 检查状态是否稳定（不需要频繁监控）
    /// </summary>
    /// <param name="state">设备状态</param>
    /// <returns>是否稳定</returns>
    public static bool IsStable(this EquipmentState state)
    {
        return state switch
        {
            EquipmentState.IDLE => true,
            EquipmentState.MAINTENANCE => true,
            EquipmentState.DOWN => true,
            _ => false
        };
    }

    /// <summary>
    /// 检查状态是否为过渡状态（临时状态）
    /// </summary>
    /// <param name="state">设备状态</param>
    /// <returns>是否为过渡状态</returns>
    public static bool IsTransitional(this EquipmentState state)
    {
        return state switch
        {
            EquipmentState.SETUP => true,
            EquipmentState.PAUSE => true,
            _ => false
        };
    }
}

/// <summary>
/// 紧急程度级别枚举
/// </summary>
public enum UrgencyLevel
{
    /// <summary>
    /// 无紧急
    /// </summary>
    None = 0,

    /// <summary>
    /// 低紧急
    /// </summary>
    Low = 1,

    /// <summary>
    /// 中等紧急
    /// </summary>
    Medium = 2,

    /// <summary>
    /// 高紧急
    /// </summary>
    High = 3,

    /// <summary>
    /// 关键紧急
    /// </summary>
    Critical = 4
}
