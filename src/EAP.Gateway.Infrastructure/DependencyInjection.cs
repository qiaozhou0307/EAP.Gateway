using EAP.Gateway.Core.Repositories;
using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using EAP.Gateway.Core.Models;
using EAP.Gateway.Core.Services;
using EAP.Gateway.Infrastructure.Caching;
using EAP.Gateway.Infrastructure.Communications.SecsGem;
using EAP.Gateway.Infrastructure.Configuration;
using EAP.Gateway.Infrastructure.Extensions;
using EAP.Gateway.Infrastructure.HostedServices;
using EAP.Gateway.Infrastructure.Messaging.Kafka;
using EAP.Gateway.Infrastructure.Messaging.RabbitMQ;
using EAP.Gateway.Infrastructure.Persistence;
using EAP.Gateway.Infrastructure.Persistence.Contexts;
using EAP.Gateway.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Secs4Net;
using StackExchange.Redis;

namespace EAP.Gateway.Infrastructure;

/// <summary>
/// 基础设施层依赖注入配置（生命周期冲突修复版本）
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
        AddSecsGemServices(services, configuration);
        AddHealthChecks(services, configuration);
        AddDicingMachineServices(services, configuration);

        return services;
    }

    /// <summary>
    /// 修复：添加数据持久化服务 - 解决DbContext生命周期问题
    /// </summary>
    private static void AddPersistence(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Database connection string 'DefaultConnection' not found.");

        // 1. 注册标准DbContext (Scoped) - 用于常规Web请求
        services.AddDbContext<EapGatewayDbContext>(options =>
        {
            ConfigureDbContextOptions(options, connectionString, environment);
        });

        // 2. 注册DbContextFactory (Singleton) - 用于后台服务和单例服务
        services.AddDbContextFactory<EapGatewayDbContext>(options =>
        {
            ConfigureDbContextOptions(options, connectionString, environment);
        });

        // 3. 注册仓储工厂模式 - 解决单例服务访问Scoped仓储的问题
        services.AddSingleton<IRepositoryFactory, RepositoryFactory>();

        // 4. 数据库初始化器保持Scoped
        services.AddScoped<IDatabaseInitializer, DatabaseInitializer>();
    }

    /// <summary>
    /// 统一的DbContext配置方法
    /// </summary>
    private static void ConfigureDbContextOptions(DbContextOptionsBuilder options, string connectionString, IHostEnvironment environment)
    {
        options.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.MigrationsAssembly(typeof(EapGatewayDbContext).Assembly.FullName);
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorCodesToAdd: null);
        });

        // 开发环境特定配置
        if (environment.IsDevelopment())
        {
            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
        }

        options.EnableServiceProviderCaching();
        options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    }

    /// <summary>
    /// 修复：添加消息队列服务 - 解决接口重复定义问题
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

        // ✅ 修复：明确使用Core层接口，Kafka生产者注册为单例
        services.AddSingleton<IKafkaProducerService, KafkaProducerService>();

        // RabbitMQ配置（如果需要）
        var rabbitMQSection = configuration.GetSection("RabbitMQ");
        if (rabbitMQSection.Exists())
        {
            services.Configure<RabbitMQConfig>(rabbitMQSection);

            // ✅ 修复：明确使用Core层接口
            services.AddSingleton<IRabbitMQService, RabbitMQService>();
        }
    }

    /// <summary>
    /// 修复：添加缓存服务 - 解决接口重复定义问题
    /// </summary>
    private static void AddCaching(IServiceCollection services, IConfiguration configuration)
    {
        var redisConnectionString = configuration.GetConnectionString("Redis");

        if (string.IsNullOrWhiteSpace(redisConnectionString))
        {
            // 如果Redis未配置，使用内存缓存作为后备
            services.AddMemoryCache();
            services.AddScoped<IDeviceStatusCacheService, MemoryDeviceStatusCacheService>();
            services.AddSingleton<IRedisService, NullRedisService>(); // ✅ 使用Core层接口
            return;
        }

        // Redis连接注册为单例
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

            // 连接事件监听
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

        // 分布式缓存注册
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
            options.InstanceName = "EapGateway";
        });

        // ✅ 修复：明确使用Core层接口
        services.AddSingleton<IRedisService, RedisService>();
        services.AddScoped<IDeviceStatusCacheService, DeviceStatusCacheService>();

        // 内存缓存作为二级缓存
        services.AddMemoryCache();
    }

    /// <summary>
    /// 修复：添加仓储服务 - 明确生命周期管理
    /// </summary>
    private static void AddRepositories(IServiceCollection services)
    {
        // 标准仓储注册为Scoped - 与DbContext生命周期匹配
        services.AddScoped<IEquipmentRepository, EquipmentRepository>();
        services.AddScoped<IAlarmRepository, AlarmRepository>();
        services.AddScoped<IDataVariableRepository, DataVariableRepository>();
        services.AddScoped<IMessageRepository, MessageRepository>();
    }

    /// <summary>
    /// 修复：添加SECS/GEM通信服务 - 解决生命周期冲突
    /// </summary>
    private static void AddSecsGemServices(IServiceCollection services, IConfiguration configuration)
    {
        // 配置选项
        services.Configure<SecsGemOptions>(configuration.GetSection("SecsGem"));

        // 工厂服务注册为单例 - 用于创建设备服务实例
        services.AddSingleton<ISecsDeviceServiceFactory, SecsDeviceServiceFactory>();
        services.AddSingleton<IHsmsClientFactory, HsmsClientFactory>();

        // 设备服务注册为瞬时 - 每个设备连接独立实例，避免状态冲突
        services.AddTransient<ISecsDeviceService, SecsDeviceService>();
        services.AddTransient<IHsmsClient, HsmsClient>();

        // 设备管理器注册为单例 - 管理所有设备连接
        services.AddSingleton<ISecsDeviceManager, SecsDeviceManager>();
    }

    /// <summary>
    /// 修复：添加裂片机连接管理服务 - 分离HostedService职责
    /// </summary>
    private static void AddDicingMachineServices(IServiceCollection services, IConfiguration configuration)
    {
        // 配置选项
        services.Configure<ConnectionManagerOptions>(configuration.GetSection("ConnectionManager"));

        // 连接管理器注册为单例 - 管理多设备连接状态
        services.AddSingleton<IMultiDicingMachineConnectionManager, MultiDicingMachineConnectionManager>();

        // HostedService单独注册 - 避免与业务服务的生命周期冲突
        services.AddHostedService<DicingMachineConnectionHostedService>();

        // 连接状态监控服务
        services.AddSingleton<IConnectionMonitoringService, ConnectionMonitoringService>();
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

        // 自定义设备连接健康检查
        healthChecksBuilder.AddTypeActivatedCheck<DeviceConnectionHealthCheck>(
            "device_connections",
            tags: new[] { "ready", "device" });
    }



}
