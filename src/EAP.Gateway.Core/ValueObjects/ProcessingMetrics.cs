using EAP.Gateway.Core.Common;

namespace EAP.Gateway.Core.ValueObjects;

/// <summary>
/// 处理指标值对象
/// </summary>
public class ProcessingMetrics : ValueObject
{
    /// <summary>
    /// 总处理数量
    /// </summary>
    public int TotalProcessed { get; }

    /// <summary>
    /// 成功处理数量
    /// </summary>
    public int SuccessCount { get; }

    /// <summary>
    /// 失败处理数量
    /// </summary>
    public int FailureCount { get; }

    /// <summary>
    /// 成功率（百分比，0-100）
    /// </summary>
    public double SuccessRate { get; }

    /// <summary>
    /// 平均处理时间
    /// </summary>
    public TimeSpan AverageProcessingTime { get; }

    /// <summary>
    /// 最后重置时间
    /// </summary>
    public DateTime LastResetAt { get; }

    /// <summary>
    /// 处理开始时间
    /// </summary>
    public DateTime StartedAt { get; }

    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTime LastUpdatedAt { get; }

    /// <summary>
    /// 最大处理时间
    /// </summary>
    public TimeSpan MaxProcessingTime { get; }

    /// <summary>
    /// 最小处理时间
    /// </summary>
    public TimeSpan MinProcessingTime { get; }

    /// <summary>
    /// 私有构造函数
    /// </summary>
    private ProcessingMetrics(
        int totalProcessed,
        int successCount,
        int failureCount,
        TimeSpan averageProcessingTime,
        DateTime lastResetAt,
        DateTime startedAt,
        DateTime lastUpdatedAt,
        TimeSpan maxProcessingTime,
        TimeSpan minProcessingTime)
    {
        TotalProcessed = totalProcessed;
        SuccessCount = successCount;
        FailureCount = failureCount;
        SuccessRate = totalProcessed > 0 ? (double)successCount / totalProcessed * 100.0 : 0.0;
        AverageProcessingTime = averageProcessingTime;
        LastResetAt = lastResetAt;
        StartedAt = startedAt;
        LastUpdatedAt = lastUpdatedAt;
        MaxProcessingTime = maxProcessingTime;
        MinProcessingTime = minProcessingTime;
    }

    /// <summary>
    /// 创建初始处理指标
    /// </summary>
    /// <param name="startTime">开始时间</param>
    /// <returns>初始指标</returns>
    public static ProcessingMetrics CreateInitial(DateTime? startTime = null)
    {
        var now = startTime ?? DateTime.UtcNow;
        return new ProcessingMetrics(
            totalProcessed: 0,
            successCount: 0,
            failureCount: 0,
            averageProcessingTime: TimeSpan.Zero,
            lastResetAt: now,
            startedAt: now,
            lastUpdatedAt: now,
            maxProcessingTime: TimeSpan.Zero,
            minProcessingTime: TimeSpan.Zero);
    }

    /// <summary>
    /// 记录成功处理
    /// </summary>
    /// <param name="processingTime">处理时间</param>
    /// <returns>新的指标实例</returns>
    public ProcessingMetrics RecordSuccess(TimeSpan processingTime)
    {
        var newTotalProcessed = TotalProcessed + 1;
        var newSuccessCount = SuccessCount + 1;
        var newFailureCount = FailureCount;

        // 计算新的平均处理时间
        var newAverageProcessingTime = CalculateNewAverage(AverageProcessingTime, processingTime, newTotalProcessed);

        // 更新最大最小处理时间
        var newMaxProcessingTime = MaxProcessingTime == TimeSpan.Zero ? processingTime :
            (processingTime > MaxProcessingTime ? processingTime : MaxProcessingTime);
        var newMinProcessingTime = MinProcessingTime == TimeSpan.Zero ? processingTime :
            (processingTime < MinProcessingTime ? processingTime : MinProcessingTime);

        return new ProcessingMetrics(
            newTotalProcessed,
            newSuccessCount,
            newFailureCount,
            newAverageProcessingTime,
            LastResetAt,
            StartedAt,
            DateTime.UtcNow,
            newMaxProcessingTime,
            newMinProcessingTime);
    }

    /// <summary>
    /// 记录失败处理
    /// </summary>
    /// <param name="processingTime">处理时间</param>
    /// <returns>新的指标实例</returns>
    public ProcessingMetrics RecordFailure(TimeSpan processingTime)
    {
        var newTotalProcessed = TotalProcessed + 1;
        var newSuccessCount = SuccessCount;
        var newFailureCount = FailureCount + 1;

        // 计算新的平均处理时间
        var newAverageProcessingTime = CalculateNewAverage(AverageProcessingTime, processingTime, newTotalProcessed);

        // 更新最大最小处理时间
        var newMaxProcessingTime = MaxProcessingTime == TimeSpan.Zero ? processingTime :
            (processingTime > MaxProcessingTime ? processingTime : MaxProcessingTime);
        var newMinProcessingTime = MinProcessingTime == TimeSpan.Zero ? processingTime :
            (processingTime < MinProcessingTime ? processingTime : MinProcessingTime);

        return new ProcessingMetrics(
            newTotalProcessed,
            newSuccessCount,
            newFailureCount,
            newAverageProcessingTime,
            LastResetAt,
            StartedAt,
            DateTime.UtcNow,
            newMaxProcessingTime,
            newMinProcessingTime);
    }

    /// <summary>
    /// 重置指标
    /// </summary>
    /// <returns>重置后的指标实例</returns>
    public ProcessingMetrics Reset()
    {
        var now = DateTime.UtcNow;
        return new ProcessingMetrics(
            totalProcessed: 0,
            successCount: 0,
            failureCount: 0,
            averageProcessingTime: TimeSpan.Zero,
            lastResetAt: now,
            startedAt: StartedAt,
            lastUpdatedAt: now,
            maxProcessingTime: TimeSpan.Zero,
            minProcessingTime: TimeSpan.Zero);
    }

    /// <summary>
    /// 计算新的平均处理时间
    /// </summary>
    private static TimeSpan CalculateNewAverage(TimeSpan currentAverage, TimeSpan newTime, int totalCount)
    {
        if (totalCount <= 1)
            return newTime;

        var currentTotalTicks = currentAverage.Ticks * (totalCount - 1);
        var newAverageTicks = (currentTotalTicks + newTime.Ticks) / totalCount;
        return TimeSpan.FromTicks(newAverageTicks);
    }

    /// <summary>
    /// 增加数据处理计数
    /// </summary>
    /// <returns>新的指标实例</returns>
    public ProcessingMetrics IncrementDataCount()
    {
        return RecordSuccess(TimeSpan.Zero); // 假设数据处理是成功的，处理时间为0
    }

    // 或者更精确的版本：
    /// <summary>
    /// 增加数据处理计数
    /// </summary>
    /// <param name="processingTime">处理时间</param>
    /// <returns>新的指标实例</returns>
    public ProcessingMetrics IncrementDataCount(TimeSpan? processingTime = null)
    {
        return RecordSuccess(processingTime ?? TimeSpan.FromMilliseconds(1)); // 默认1ms处理时间
    }

    /// <summary>
    /// 运行时长
    /// </summary>
    public TimeSpan RunningTime => LastUpdatedAt - StartedAt;

    /// <summary>
    /// 每秒处理数
    /// </summary>
    public double ProcessingRate
    {
        get
        {
            var runningTime = RunningTime;
            return runningTime.TotalSeconds > 0 ? TotalProcessed / runningTime.TotalSeconds : 0.0;
        }
    }

    /// <summary>
    /// 是否有处理记录
    /// </summary>
    public bool HasProcessingHistory => TotalProcessed > 0;

    /// <summary>
    /// 错误率
    /// </summary>
    public double ErrorRate => 100.0 - SuccessRate;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return TotalProcessed;
        yield return SuccessCount;
        yield return FailureCount;
        yield return AverageProcessingTime;
        yield return LastResetAt;
        yield return StartedAt;
        yield return LastUpdatedAt;
        yield return MaxProcessingTime;
        yield return MinProcessingTime;
    }

    public override string ToString()
    {
        return $"Total: {TotalProcessed}, Success: {SuccessCount}, Failure: {FailureCount}, Rate: {SuccessRate:F1}%, Avg: {AverageProcessingTime.TotalMilliseconds:F0}ms";
    }

    /// <summary>
    /// 创建空的处理指标（替代 Empty 方法）
    /// </summary>
    /// <returns>空的处理指标</returns>
    public static ProcessingMetrics Empty()
    {
        var now = DateTime.UtcNow;
        return new ProcessingMetrics(
            totalProcessed: 0,
            successCount: 0,
            failureCount: 0,
            averageProcessingTime: TimeSpan.Zero,
            lastResetAt: now,
            startedAt: now,
            lastUpdatedAt: now,
            maxProcessingTime: TimeSpan.Zero,
            minProcessingTime: TimeSpan.Zero);
    }
}
