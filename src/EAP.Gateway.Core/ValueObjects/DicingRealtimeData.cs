using EAP.Gateway.Core.Common;

namespace EAP.Gateway.Core.ValueObjects;

/// <summary>
/// 裂片机实时数据值对象
/// </summary>
public class DicingRealtimeData : ValueObject
{
    /// <summary>
    /// X轴位置
    /// </summary>
    public double X { get; }

    /// <summary>
    /// Y轴位置
    /// </summary>
    public double Y { get; }

    /// <summary>
    /// Theta角度 (θ)
    /// </summary>
    public double Theta { get; }

    /// <summary>
    /// Z轴位置
    /// </summary>
    public double Z { get; }

    /// <summary>
    /// 进度百分比 (0-100)
    /// </summary>
    public int Progress { get; }

    /// <summary>
    /// 晶圆当前数
    /// </summary>
    public int CurrentWaferCount { get; }

    /// <summary>
    /// 晶圆总数
    /// </summary>
    public int TotalWaferCount { get; }

    /// <summary>
    /// 劈裂数
    /// </summary>
    public int DicingCount { get; }

    /// <summary>
    /// 受台数
    /// </summary>
    public int StageCount { get; }

    /// <summary>
    /// 数据时间戳
    /// </summary>
    public DateTime Timestamp { get; }

    public DicingRealtimeData(
        double x, double y, double theta, double z,
        int progress, int currentWaferCount, int totalWaferCount,
        int dicingCount, int stageCount)
    {
        X = x;
        Y = y;
        Theta = theta;
        Z = z;
        Progress = Math.Max(0, Math.Min(100, progress)); // 确保在0-100范围内
        CurrentWaferCount = Math.Max(0, currentWaferCount);
        TotalWaferCount = Math.Max(0, totalWaferCount);
        DicingCount = Math.Max(0, dicingCount);
        StageCount = Math.Max(0, stageCount);
        Timestamp = DateTime.UtcNow;
    }

    public static DicingRealtimeData Empty() => new(0, 0, 0, 0, 0, 0, 0, 0, 0);

    /// <summary>
    /// 计算完成百分比
    /// </summary>
    public double CalculateCompletionPercentage()
    {
        if (TotalWaferCount == 0) return 0;
        return (double)CurrentWaferCount / TotalWaferCount * 100;
    }

    /// <summary>
    /// 是否已完成所有晶圆处理
    /// </summary>
    public bool IsCompleted => CurrentWaferCount >= TotalWaferCount && TotalWaferCount > 0;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return X;
        yield return Y;
        yield return Theta;
        yield return Z;
        yield return Progress;
        yield return CurrentWaferCount;
        yield return TotalWaferCount;
        yield return DicingCount;
        yield return StageCount;
    }
}
