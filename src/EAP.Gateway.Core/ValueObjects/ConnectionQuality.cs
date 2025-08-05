namespace EAP.Gateway.Core.ValueObjects;

/// <summary>
/// 连接质量枚举
/// </summary>
public enum ConnectionQuality
{
    /// <summary>
    /// 未知质量
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// 差质量 - 连接不稳定，可能需要重连
    /// </summary>
    Poor = 1,

    /// <summary>
    /// 一般质量 - 连接基本正常，偶有延迟
    /// </summary>
    Fair = 2,

    /// <summary>
    /// 良好质量 - 连接稳定，延迟正常
    /// </summary>
    Good = 3,

    /// <summary>
    /// 优秀质量 - 连接非常稳定，延迟很低
    /// </summary>
    Excellent = 4
}
