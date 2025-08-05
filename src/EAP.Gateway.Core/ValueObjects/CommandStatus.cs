namespace EAP.Gateway.Core.ValueObjects;

/// <summary>
/// 命令状态枚举
/// </summary>
public enum CommandStatus
{
    /// <summary>
    /// 等待中
    /// </summary>
    Pending = 0,

    /// <summary>
    /// 执行中
    /// </summary>
    Executing = 1,

    /// <summary>
    /// 已完成
    /// </summary>
    Completed = 2,

    /// <summary>
    /// 执行失败
    /// </summary>
    Failed = 3,

    /// <summary>
    /// 已取消
    /// </summary>
    Cancelled = 4,

    /// <summary>
    /// 执行超时
    /// </summary>
    Timeout = 5
}
