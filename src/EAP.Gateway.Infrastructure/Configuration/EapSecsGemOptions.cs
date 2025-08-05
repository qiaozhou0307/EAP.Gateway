namespace EAP.Gateway.Infrastructure.Configuration;

/// <summary>
/// EAP SECS/GEM配置选项（重命名以避免与Secs4Net冲突）
/// </summary>
public class EapSecsGemOptions
{
    public const string SectionName = "SecsGem";

    public int MaxConcurrentMessages { get; set; } = 10;
    public bool EnableDetailedLogging { get; set; } = false;
    public bool EnableMetrics { get; set; } = true;
    public RetryOptions RetryOptions { get; set; } = new();
    public TimeoutOptions TimeoutOptions { get; set; } = new();
}

public class RetryOptions
{
    public int MaxRetries { get; set; } = 3;
    public TimeSpan BaseDelay { get; set; } = TimeSpan.FromSeconds(1);
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(30);
}

public class TimeoutOptions
{
    public TimeSpan T3Timeout { get; set; } = TimeSpan.FromSeconds(45);
    public TimeSpan T5Timeout { get; set; } = TimeSpan.FromSeconds(10);
    public TimeSpan T6Timeout { get; set; } = TimeSpan.FromSeconds(5);
    public TimeSpan T7Timeout { get; set; } = TimeSpan.FromSeconds(10);
    public TimeSpan T8Timeout { get; set; } = TimeSpan.FromSeconds(6);
}
