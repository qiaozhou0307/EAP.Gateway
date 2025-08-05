namespace EAP.Gateway.Core.ValueObjects;

/// <summary>
/// 设备类型枚举 - 支持四种设备类型
/// </summary>
public enum EquipmentType
{
    /// <summary>
    /// 通用设备
    /// </summary>
    Generic = 0,

    /// <summary>
    /// 划片机 (Scribe Machine)
    /// </summary>
    ScribeMachine = 1,

    /// <summary>
    /// 裂片机 (Dicing Machine) 
    /// </summary>
    DicingMachine = 2,

    /// <summary>
    /// 测试机 (Test Machine)
    /// </summary>
    TestMachine = 3,

    /// <summary>
    /// AOI检测机 (Automated Optical Inspection)
    /// </summary>
    AOIMachine = 4
}
