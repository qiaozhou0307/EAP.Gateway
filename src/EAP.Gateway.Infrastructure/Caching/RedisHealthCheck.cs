using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace EAP.Gateway.Infrastructure.Caching;

/// <summary>
/// Redis健康检查
/// </summary>
public class RedisHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly ILogger<RedisHealthCheck> _logger;

    public RedisHealthCheck(IConnectionMultiplexer connectionMultiplexer, ILogger<RedisHealthCheck> logger)
    {
        _connectionMultiplexer = connectionMultiplexer;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var database = _connectionMultiplexer.GetDatabase();

            // 执行简单的ping测试
            var response = await database.PingAsync();

            if (response.TotalMilliseconds > 1000) // 如果延迟超过1秒，标记为不健康
            {
                return HealthCheckResult.Degraded($"Redis响应延迟过高: {response.TotalMilliseconds}ms");
            }

            var data = new Dictionary<string, object>
            {
                ["ping_response_ms"] = response.TotalMilliseconds,
                ["connected_endpoints"] = _connectionMultiplexer.GetEndPoints().Length,
                ["is_connected"] = _connectionMultiplexer.IsConnected
            };

            return HealthCheckResult.Healthy("Redis连接正常", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis健康检查失败");
            return HealthCheckResult.Unhealthy("Redis连接失败", ex);
        }
    }
}
