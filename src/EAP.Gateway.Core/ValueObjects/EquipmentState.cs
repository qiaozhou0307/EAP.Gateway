namespace EAP.Gateway.Core.ValueObjects;

/// <summary>
/// 设备状态枚举，符合SEMI E30标准
/// </summary>
public enum EquipmentState
{
    /// <summary>
    /// 未知状态 - 初始状态或通信异常
    /// </summary>
    UNKNOWN = 0,

    /// <summary>
    /// 空闲状态 - 设备可用但未处理任何批次
    /// </summary>
    IDLE = 1,

    /// <summary>
    /// 设置状态 - 设备正在进行设置或配置
    /// </summary>
    SETUP = 2,

    /// <summary>
    /// 执行状态 - 设备正在处理批次
    /// </summary>
    EXECUTING = 3,

    /// <summary>
    /// 暂停状态 - 设备暂停处理，可恢复
    /// </summary>
    PAUSE = 4,

    /// <summary>
    /// 停机状态 - 设备因故障或维护停机
    /// </summary>
    DOWN = 5,

    /// <summary>
    /// 维护状态 - 设备正在维护中
    /// </summary>
    MAINTENANCE = 6,

    /// <summary>
    /// 故障状态 - 设备发生故障，需要干预
    /// </summary>
    FAULT = 7,

    /// <summary>
    /// 报警状态 - 设备有活动报警
    /// </summary>
    ALARM = 8
}

