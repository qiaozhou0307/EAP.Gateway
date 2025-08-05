namespace EAP.Gateway.Core.ValueObjects;

/// <summary>
/// 设备服务状态枚举
/// </summary>
public enum DeviceServiceStatus
{
    /// <summary>
    /// 未初始化
    /// </summary>
    NotInitialized = 0,

    /// <summary>
    /// 初始化中
    /// </summary>
    Initializing = 1,

    /// <summary>
    /// 已初始化但未启动
    /// </summary>
    Initialized = 2,

    /// <summary>
    /// 启动中
    /// </summary>
    Starting = 3,

    /// <summary>
    /// 已启动运行中
    /// </summary>
    Started = 4,

    /// <summary>
    /// 停止中
    /// </summary>
    Stopping = 5,

    /// <summary>
    /// 已停止
    /// </summary>
    Stopped = 6,

    /// <summary>
    /// 出现错误
    /// </summary>
    Error = 7,

    /// <summary>
    /// 故障状态（新增）
    /// </summary>
    Faulted = 8,

    /// <summary>
    /// 已释放
    /// </summary>
    Disposed = 9
}
/// <summary>
/// 设备服务状态扩展方法
/// </summary>
public static class DeviceServiceStatusExtensions
{
    /// <summary>
    /// 检查状态是否为运行状态
    /// </summary>
    public static bool IsRunning(this DeviceServiceStatus status)
    {
        return status == DeviceServiceStatus.Started;
    }

    /// <summary>
    /// 检查状态是否为错误状态
    /// </summary>
    public static bool IsErrorState(this DeviceServiceStatus status)
    {
        return status == DeviceServiceStatus.Error ||
               status == DeviceServiceStatus.Faulted;
    }

    /// <summary>
    /// 检查状态是否为终止状态
    /// </summary>
    public static bool IsTerminalState(this DeviceServiceStatus status)
    {
        return status == DeviceServiceStatus.Stopped ||
               status == DeviceServiceStatus.Disposed ||
               status == DeviceServiceStatus.Faulted;
    }

    /// <summary>
    /// 检查是否可以启动
    /// </summary>
    public static bool CanStart(this DeviceServiceStatus status)
    {
        return status == DeviceServiceStatus.NotInitialized ||
               status == DeviceServiceStatus.Initialized ||
               status == DeviceServiceStatus.Stopped;
    }

    /// <summary>
    /// 检查是否可以停止
    /// </summary>
    public static bool CanStop(this DeviceServiceStatus status)
    {
        return status == DeviceServiceStatus.Started ||
               status == DeviceServiceStatus.Starting ||
               status == DeviceServiceStatus.Error ||
               status == DeviceServiceStatus.Faulted;
    }

    /// <summary>
    /// 获取状态显示名称
    /// </summary>
    public static string GetDisplayName(this DeviceServiceStatus status)
    {
        return status switch
        {
            DeviceServiceStatus.NotInitialized => "未初始化",
            DeviceServiceStatus.Initializing => "初始化中",
            DeviceServiceStatus.Initialized => "已初始化",
            DeviceServiceStatus.Starting => "启动中",
            DeviceServiceStatus.Started => "运行中",
            DeviceServiceStatus.Stopping => "停止中",
            DeviceServiceStatus.Stopped => "已停止",
            DeviceServiceStatus.Error => "错误",
            DeviceServiceStatus.Faulted => "故障",
            DeviceServiceStatus.Disposed => "已释放",
            _ => status.ToString()
        };
    }
}
