using EAP.Gateway.Core.Repositories;
using EAP.Gateway.Infrastructure.Caching.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace EAP.Gateway.Infrastructure.Caching.Extensions;

/// <summary>
/// 缓存服务注册扩展方法
/// </summary>
public static class CachingServiceCollectionExtensions
{
    /// <summary>
    /// 添加增强版Redis缓存服务
    /// </summary>
    public static IServiceCollection AddEnhancedRedisCache(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 1. 配置选项
        services.Configure<RedisOptions>(configuration.GetSection(RedisOptions.SectionName));
        var redisOptions = configuration.GetSection(RedisOptions.SectionName).Get<RedisOptions>();

        if (redisOptions == null || string.IsNullOrWhiteSpace(redisOptions.ConnectionString))
        {
            throw new InvalidOperationException("Redis配置未正确设置");
        }

        // 2. 注册 StackExchange.Redis IConnectionMultiplexer
        services.AddSingleton<IConnectionMultiplexer>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<IConnectionMultiplexer>>();

            try
            {
                var configOptions = ConfigurationOptions.Parse(redisOptions.ConnectionString);
                configOptions.AbortOnConnectFail = redisOptions.AbortOnConnectFail;
                configOptions.ConnectRetry = redisOptions.ConnectRetry;
                configOptions.ConnectTimeout = redisOptions.ConnectTimeoutMs;
                configOptions.SyncTimeout = redisOptions.SyncTimeoutMs;
                configOptions.AsyncTimeout = redisOptions.AsyncTimeoutMs;

                var multiplexer = ConnectionMultiplexer.Connect(configOptions);

                // 事件处理
                multiplexer.ConnectionFailed += (sender, args) =>
                {
                    logger.LogError("Redis连接失败: {EndPoint}, {FailureType}", args.EndPoint, args.FailureType);
                };

                multiplexer.ConnectionRestored += (sender, args) =>
                {
                    logger.LogInformation("Redis连接恢复: {EndPoint}", args.EndPoint);
                };

                logger.LogInformation("Redis连接建立成功");
                return multiplexer;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Redis连接建立失败");
                throw;
            }
        });

        // 3. 注册 Microsoft.Extensions.Caching.Distributed
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisOptions.ConnectionString;
            options.InstanceName = redisOptions.InstanceName;
        });

        // 4. 注册Redis指标和健康检查
        services.AddSingleton<RedisMetrics>();
        services.AddTransient<RedisHealthCheck>();

        // 5. 注册增强版Redis服务
        services.AddSingleton<IRedisService, EnhancedRedisService>();

        // 6. 注册设备状态缓存服务
        services.AddScoped<IDeviceStatusCacheService, DeviceStatusCacheService>();

        return services;
    }
}
