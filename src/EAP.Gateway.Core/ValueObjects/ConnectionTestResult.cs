using EAP.Gateway.Core.Common;

namespace EAP.Gateway.Core.ValueObjects;

/// <summary>
/// 连接测试结果值对象
/// 连接测试是业务概念，应该属于领域层
/// </summary>
public sealed class ConnectionTestResult : ValueObject
{
    public bool IsSuccessful { get; }
    public TimeSpan ResponseTime { get; }
    public string? ErrorMessage { get; }
    public DateTime TestedAt { get; }
    public ConnectionTestType TestType { get; }

    private ConnectionTestResult(bool isSuccessful, TimeSpan responseTime, string? errorMessage, ConnectionTestType testType)
    {
        IsSuccessful = isSuccessful;
        ResponseTime = responseTime;
        ErrorMessage = errorMessage;
        TestedAt = DateTime.UtcNow;
        TestType = testType;
    }

    public static ConnectionTestResult Success(TimeSpan responseTime, ConnectionTestType testType = ConnectionTestType.Basic) =>
        new(true, responseTime, null, testType);

    public static ConnectionTestResult Failure(string errorMessage, TimeSpan responseTime = default,
        ConnectionTestType testType = ConnectionTestType.Basic) =>
        new(false, responseTime, errorMessage, testType);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return IsSuccessful;
        yield return ResponseTime;
        yield return ErrorMessage;
        yield return TestType;
    }

    public override string ToString()
    {
        return IsSuccessful
            ? $"连接测试成功 (响应时间: {ResponseTime.TotalMilliseconds:F1}ms)"
            : $"连接测试失败: {ErrorMessage}";
    }
}

/// <summary>
/// 连接测试类型
/// </summary>
public enum ConnectionTestType
{
    /// <summary>
    /// 基础连接测试
    /// </summary>
    Basic,

    /// <summary>
    /// 通信建立测试
    /// </summary>
    Communication,

    /// <summary>
    /// 深度健康检查
    /// </summary>
    HealthCheck,

    /// <summary>
    /// 性能测试
    /// </summary>
    Performance
}
