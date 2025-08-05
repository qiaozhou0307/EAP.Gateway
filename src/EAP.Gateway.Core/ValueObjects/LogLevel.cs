namespace EAP.Gateway.Core.ValueObjects;

/// <summary>
/// 日志级别枚举
/// </summary>
public enum LogLevel
{
    /// <summary>
    /// 跟踪
    /// </summary>
    Trace = 0,

    /// <summary>
    /// 调试
    /// </summary>
    Debug = 1,

    /// <summary>
    /// 信息
    /// </summary>
    Information = 2,

    /// <summary>
    /// 警告
    /// </summary>
    Warning = 3,

    /// <summary>
    /// 错误
    /// </summary>
    Error = 4,

    /// <summary>
    /// 严重错误
    /// </summary>
    Critical = 5,

    /// <summary>
    /// 无日志
    /// </summary>
    None = 6
}
