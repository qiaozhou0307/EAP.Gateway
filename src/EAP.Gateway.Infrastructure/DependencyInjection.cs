using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using EAP.Gateway.Core.Models;
using EAP.Gateway.Core.Repositories;
using EAP.Gateway.Core.Services;
using EAP.Gateway.Infrastructure.Caching;
using EAP.Gateway.Infrastructure.Communications.SecsGem;
using EAP.Gateway.Infrastructure.Configuration;
using EAP.Gateway.Infrastructure.Extensions;
using EAP.Gateway.Infrastructure.Messaging.Kafka;
using EAP.Gateway.Infrastructure.Persistence;
using EAP.Gateway.Infrastructure.Persistence.Contexts;
using EAP.Gateway.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace EAP.Gateway.Infrastructure;

/// <summary>
/// 基础设施层依赖注入配置（最终修复版本）
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// 注册基础设施层服务
    /// </summary>
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        AddPersistence(services, configuration, environment);
        AddCaching(services, configuration);
        AddRepositories(services);
        AddMessaging(services, configuration);
        AddSecsGemServices(services);
        AddHealthChecks(services, configuration);


        AddDicingMachineServices(services, configuration);
        return services;
    }

    /// <summary>
    /// 添加数据持久化服务
    /// </summary>
    private static void AddPersistence(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddDbContext<EapGatewayDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(EapGatewayDbContext).Assembly.FullName);
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorCodesToAdd: null);
            });

            options.EnableSensitiveDataLogging(environment.IsDevelopment());
            options.EnableServiceProviderCaching();
            options.EnableDetailedErrors(environment.IsDevelopment());
            options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        });

        services.AddScoped<IDatabaseInitializer, DatabaseInitializer>();
    }

    /// <summary>
    /// 添加缓存服务（最终修复版本）
    /// </summary>
    private static void AddCaching(IServiceCollection services, IConfiguration configuration)
    {
        var redisConnectionString = configuration.GetConnectionString("Redis");
        if (string.IsNullOrWhiteSpace(redisConnectionString))
        {
            throw new InvalidOperationException("Redis连接字符串未配置");
        }

        // 注册 StackExchange.Redis IConnectionMultiplexer
        services.AddSingleton<IConnectionMultiplexer>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<IConnectionMultiplexer>>();

            var options = ConfigurationOptions.Parse(redisConnectionString);
            options.AbortOnConnectFail = false;
            options.ConnectRetry = 3;
            options.ConnectTimeout = 5000;
            options.SyncTimeout = 5000;
            options.AsyncTimeout = 5000;

            var multiplexer = ConnectionMultiplexer.Connect(options);

            multiplexer.ConnectionFailed += (_, args) =>
            {
                logger.LogError("Redis连接失败: {EndPoint}, {FailureType}", args.EndPoint, args.FailureType);
            };

            multiplexer.ConnectionRestored += (_, args) =>
            {
                logger.LogInformation("Redis连接恢复: {EndPoint}", args.EndPoint);
            };

            return multiplexer;
        });

        // 注册 Microsoft.Extensions.Caching.Distributed
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
            options.InstanceName = "EapGateway";
        });

        // 注册Redis服务
        services.AddSingleton<IRedisService, RedisService>();
        services.AddScoped<IDeviceStatusCacheService, DeviceStatusCacheService>();
        services.AddMemoryCache();
    }

    /// <summary>
    /// 添加仓储服务
    /// </summary>
    private static void AddRepositories(IServiceCollection services)
    {
        services.AddScoped<IEquipmentRepository, EquipmentRepository>();
    }

    /// <summary>
    /// 添加消息队列服务
    /// </summary>
    private static void AddMessaging(IServiceCollection services, IConfiguration configuration)
    {
        // Kafka配置验证
        var kafkaSection = configuration.GetSection("Kafka");
        if (kafkaSection.Exists())
        {
            var kafkaConfig = kafkaSection.Get<KafkaConfig>();
            if (kafkaConfig != null)
            {
                var (isValid, errors) = kafkaConfig.Validate();
                if (!isValid)
                {
                    throw new InvalidOperationException($"Kafka配置无效: {string.Join(", ", errors)}");
                }
            }
        }

        services.Configure<KafkaConfig>(kafkaSection);
        services.AddSingleton<IKafkaProducerService, KafkaProducerService>();
    }

    /// <summary>
    /// 添加 SECS/GEM 通信服务
    /// </summary>
    private static void AddSecsGemServices(IServiceCollection services)
    {
        services.AddTransient<IHsmsClient, HsmsClient>();
        services.AddTransient<ISecsDeviceService, SecsDeviceService>();
        services.AddSingleton<ISecsDeviceServiceFactory, SecsDeviceServiceFactory>();
        services.AddSingleton<ISecsDeviceManager, SecsDeviceManager>();
    }

    /// <summary>
    /// 添加健康检查
    /// </summary>
    private static void AddHealthChecks(IServiceCollection services, IConfiguration configuration)
    {
        var healthChecksBuilder = services.AddHealthChecks();

        // PostgreSQL健康检查
        var dbConnection = configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrWhiteSpace(dbConnection))
        {
            healthChecksBuilder.AddNpgSql(dbConnection, name: "postgresql", tags: new[] { "ready", "database" });
        }

        // Redis健康检查
        var redisConnection = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redisConnection))
        {
            healthChecksBuilder.AddRedis(redisConnection, name: "redis", tags: new[] { "ready", "cache" });
        }

        // Kafka健康检查
        var kafkaServers = configuration.GetSection("Kafka:BootstrapServers").Value;
        if (!string.IsNullOrWhiteSpace(kafkaServers))
        {
            healthChecksBuilder.AddKafka(options =>
            {
                options.BootstrapServers = kafkaServers;
            }, name: "kafka", tags: new[] { "ready", "messaging" });
        }
    }


    /// <summary>
    /// 添加裂片机连接管理服务
    /// </summary>
    public static IServiceCollection AddDicingMachineServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 注册多设备连接管理器
        services.AddSingleton<IMultiDicingMachineConnectionManager, MultiDicingMachineConnectionManager>();

        // 注册设备服务工厂（如果还没注册）
        services.AddSingleton<ISecsDeviceServiceFactory, SecsDeviceServiceFactory>();

        // 注册健康检查
        services.AddSingleton<DicingMachineHealthCheck>();
        services.AddHealthChecks()
            .AddTypeActivatedCheck<DicingMachineHealthCheck>("dicing-machines");

        // 验证并绑定配置
        var dicingMachineSection = configuration.GetSection("DicingMachines");
        if (dicingMachineSection.Exists())
        {
            services.Configure<DicingMachinesOptions>(dicingMachineSection);

            // 验证配置有效性
            var configs = dicingMachineSection.GetSection("Devices").Get<DicingMachineConfig[]>();
            if (configs != null)
            {
                foreach (var config in configs)
                {
                    var (isValid, errors) = config.Validate();
                    if (!isValid)
                    {
                        throw new InvalidOperationException($"裂片机配置无效 [{config.Name}]: {string.Join(", ", errors)}");
                    }
                }
            }
        }

        return services;
    }

    /// <summary>
    /// 裂片机配置选项
    /// </summary>
    public class DicingMachinesOptions
    {
        public DicingMachineConfig[] Devices { get; set; } = Array.Empty<DicingMachineConfig>();
        public GlobalDicingMachineSettings GlobalSettings { get; set; } = new();
    }

    /// <summary>
    /// 全局裂片机设置
    /// </summary>
    public class GlobalDicingMachineSettings
    {
        public int MaxConcurrentConnections { get; set; } = 5;
        public TimeSpan DefaultConnectionTimeout { get; set; } = TimeSpan.FromSeconds(30);
        public bool AutoReconnectEnabled { get; set; } = true;
        public TimeSpan MonitoringInterval { get; set; } = TimeSpan.FromMinutes(1);
        public TimeSpan HealthCheckInterval { get; set; } = TimeSpan.FromMinutes(2);
    }
}
