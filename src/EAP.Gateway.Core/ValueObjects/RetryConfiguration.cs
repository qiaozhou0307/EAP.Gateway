using EAP.Gateway.Core.Common;

namespace EAP.Gateway.Core.ValueObjects;

/// <summary>
/// 重试配置
/// </summary>
public class RetryConfiguration : ValueObject
{
    /// <summary>
    /// 最大重试次数
    /// </summary>
    public int MaxRetries { get; }

    /// <summary>
    /// 初始延迟（毫秒）
    /// </summary>
    public int InitialDelay { get; }

    /// <summary>
    /// 延迟倍数（指数退避）
    /// </summary>
    public double DelayMultiplier { get; }

    /// <summary>
    /// 最大延迟（毫秒）
    /// </summary>
    public int MaxDelay { get; }

    /// <summary>
    /// 是否启用抖动
    /// </summary>
    public bool EnableJitter { get; }

    public RetryConfiguration(
        int maxRetries = 3,
        int initialDelay = 1000,
        double delayMultiplier = 2.0,
        int maxDelay = 30000,
        bool enableJitter = true)
    {
        MaxRetries = maxRetries >= 0 ? maxRetries : throw new ArgumentException("Max retries must be non-negative", nameof(maxRetries));
        InitialDelay = initialDelay > 0 ? initialDelay : throw new ArgumentException("Initial delay must be positive", nameof(initialDelay));
        DelayMultiplier = delayMultiplier > 0 ? delayMultiplier : throw new ArgumentException("Delay multiplier must be positive", nameof(delayMultiplier));
        MaxDelay = maxDelay > 0 ? maxDelay : throw new ArgumentException("Max delay must be positive", nameof(maxDelay));
        EnableJitter = enableJitter;
    }

    /// <summary>
    /// 计算重试延迟
    /// </summary>
    /// <param name="attemptNumber">尝试次数（从0开始）</param>
    /// <returns>延迟时间（毫秒）</returns>
    public int CalculateDelay(int attemptNumber)
    {
        if (attemptNumber < 0)
            return InitialDelay;

        var delay = (int)(InitialDelay * Math.Pow(DelayMultiplier, attemptNumber));
        delay = Math.Min(delay, MaxDelay);

        if (EnableJitter)
        {
            var jitter = new Random().NextDouble() * 0.1; // ±10%的抖动
            delay = (int)(delay * (1 + jitter - 0.05));
        }

        return delay;
    }

    /// <summary>
    /// 创建默认重试配置
    /// </summary>
    /// <returns>默认配置</returns>
    public static RetryConfiguration Default() => new();

    /// <summary>
    /// 创建无重试配置
    /// </summary>
    /// <returns>无重试配置</returns>
    public static RetryConfiguration NoRetry() => new(0);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return MaxRetries;
        yield return InitialDelay;
        yield return DelayMultiplier;
        yield return MaxDelay;
        yield return EnableJitter;
    }
}
