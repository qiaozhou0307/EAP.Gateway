namespace EAP.Gateway.Core.ValueObjects;

/// <summary>
/// 报警状态枚举
/// </summary>
public enum AlarmState
{
    /// <summary>
    /// 已设置 - 报警刚刚触发
    /// </summary>
    Set = 0,

    /// <summary>
    /// 已确认 - 操作员已确认报警但未清除
    /// </summary>
    Acknowledged = 1,

    /// <summary>
    /// 已清除 - 报警条件已消除
    /// </summary>
    Cleared = 2
}
