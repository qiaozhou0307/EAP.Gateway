using EAP.Gateway.Core.Common;

namespace EAP.Gateway.Core.ValueObjects;

/// <summary>
/// HSMS超时配置
/// </summary>
public class HsmsTimeouts : ValueObject
{
    /// <summary>
    /// T3 - Reply timeout (秒)
    /// </summary>
    public int T3 { get; }

    /// <summary>
    /// T5 - Connect separation time (秒)
    /// </summary>
    public int T5 { get; }

    /// <summary>
    /// T6 - Control transaction timeout (秒)
    /// </summary>
    public int T6 { get; }

    /// <summary>
    /// T7 - NOT connected timeout (秒)
    /// </summary>
    public int T7 { get; }

    /// <summary>
    /// T8 - Network inter-character timeout (秒)
    /// </summary>
    public int T8 { get; }

    public HsmsTimeouts(int t3 = 45, int t5 = 10, int t6 = 5, int t7 = 10, int t8 = 6)
    {
        T3 = t3 > 0 ? t3 : throw new ArgumentException("T3 must be positive", nameof(t3));
        T5 = t5 > 0 ? t5 : throw new ArgumentException("T5 must be positive", nameof(t5));
        T6 = t6 > 0 ? t6 : throw new ArgumentException("T6 must be positive", nameof(t6));
        T7 = t7 > 0 ? t7 : throw new ArgumentException("T7 must be positive", nameof(t7));
        T8 = t8 > 0 ? t8 : throw new ArgumentException("T8 must be positive", nameof(t8));
    }

    /// <summary>
    /// 创建默认超时配置
    /// </summary>
    /// <returns>默认配置</returns>
    public static HsmsTimeouts Default() => new();

    /// <summary>
    /// 创建快速超时配置（用于测试）
    /// </summary>
    /// <returns>快速配置</returns>
    public static HsmsTimeouts Fast() => new(5, 2, 1, 2, 1);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return T3;
        yield return T5;
        yield return T6;
        yield return T7;
        yield return T8;
    }
}
