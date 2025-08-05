using EAP.Gateway.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EAP.Gateway.Api.Extensions;

/// <summary>
/// 健康检查扩展（修复版本）
/// </summary>
public static class HealthCheckExtensions
{
    /// <summary>
    /// 添加自定义健康检查
    /// </summary>
    public static IServiceCollection AddCustomHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        var healthChecksBuilder = services.AddHealthChecks();

        // 修复：AddDbContext健康检查的正确写法
        // 注意：AddDbContext扩展方法没有name参数，需要使用AddCheck或其他方式
        healthChecksBuilder.AddCheck<DatabaseHealthCheck>(
            name: "database",
            failureStatus: HealthStatus.Unhealthy,
            tags: new[] { "ready", "database" });

        // Redis健康检查
        var redisConnectionString = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            healthChecksBuilder.AddRedis(
                redisConnectionString,
                name: "redis",
                failureStatus: HealthStatus.Unhealthy,
                tags: new[] { "ready", "cache" });
        }

        // Kafka健康检查
        var kafkaBootstrapServers = configuration.GetSection("Kafka:BootstrapServers").Value;
        if (!string.IsNullOrWhiteSpace(kafkaBootstrapServers))
        {
            healthChecksBuilder.AddKafka(options =>
            {
                options.BootstrapServers = kafkaBootstrapServers;
            },
            name: "kafka",
            failureStatus: HealthStatus.Degraded,
            tags: new[] { "ready", "messaging" });
        }

        // PostgreSQL健康检查
        var dbConnectionString = configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrWhiteSpace(dbConnectionString))
        {
            healthChecksBuilder.AddNpgSql(
                dbConnectionString,
                name: "postgresql",
                failureStatus: HealthStatus.Unhealthy,
                tags: new[] { "ready", "database" });
        }

        return services;
    }
}
/// <summary>
/// 自定义数据库健康检查
/// </summary>
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly EapGatewayDbContext _context;
    private readonly ILogger<DatabaseHealthCheck> _logger;

    public DatabaseHealthCheck(EapGatewayDbContext context, ILogger<DatabaseHealthCheck> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // 执行简单的数据库连接测试
            var canConnect = await _context.Database.CanConnectAsync(cancellationToken);

            if (!canConnect)
            {
                return HealthCheckResult.Unhealthy("无法连接到数据库");
            }

            // 检查是否可以执行查询
            var equipmentCount = await _context.Equipment.CountAsync(cancellationToken);

            var data = new Dictionary<string, object>
            {
                ["equipment_count"] = equipmentCount,
                ["database_provider"] = _context.Database.ProviderName ?? "Unknown",
                ["can_connect"] = canConnect
            };

            return HealthCheckResult.Healthy("数据库连接正常", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "数据库健康检查失败");
            return HealthCheckResult.Unhealthy("数据库连接失败", ex);
        }
    }
}
