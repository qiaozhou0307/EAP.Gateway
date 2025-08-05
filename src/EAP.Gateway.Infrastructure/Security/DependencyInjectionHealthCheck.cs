using EAP.Gateway.Core.Repositories;
using EAP.Gateway.Core.Services;
using EAP.Gateway.Infrastructure.Communications.SecsGem;
using EAP.Gateway.Infrastructure.Persistence.Contexts;
using EAP.Gateway.Infrastructure.Persistence.Factories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace EAP.Gateway.Infrastructure.HealthChecks;

/// <summary>
/// 依赖注入健康检查 - 验证服务生命周期配置是否正确
/// </summary>
public class DependencyInjectionHealthCheck : IHealthCheck
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DependencyInjectionHealthCheck> _logger;

    public DependencyInjectionHealthCheck(
        IServiceProvider serviceProvider,
        ILogger<DependencyInjectionHealthCheck> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var healthData = new Dictionary<string, object>();
        var issues = new List<string>();

        try
        {
            // 1. 检查DbContext配置
            await CheckDbContextConfigurationAsync(healthData, issues);

            // 2. 检查仓储工厂模式
            await CheckRepositoryFactoryPatternAsync(healthData, issues);

            // 3. 检查SECS/GEM服务配置
            CheckSecsGemServicesConfiguration(healthData, issues);

            // 4. 检查连接管理器配置
            CheckConnectionManagerConfiguration(healthData, issues);

            // 5. 检查后台服务配置
            CheckHostedServicesConfiguration(healthData, issues);

            // 6. 检查缓存服务配置
            CheckCacheServicesConfiguration(healthData, issues);

            if (issues.Any())
            {
                var issueDescription = string.Join("; ", issues);
                _logger.LogWarning("依赖注入配置存在问题: {Issues}", issueDescription);

                return HealthCheckResult.Degraded(
                    description: $"发现 {issues.Count} 个配置问题",
                    data: healthData);
            }

            _logger.LogDebug("依赖注入配置健康检查通过");
            return HealthCheckResult.Healthy("所有依赖注入配置正常", healthData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "依赖注入健康检查异常");
            return HealthCheckResult.Unhealthy("依赖注入健康检查失败", ex, healthData);
        }
    }

    /// <summary>
    /// 检查DbContext配置
    /// </summary>
    private async Task CheckDbContextConfigurationAsync(Dictionary<string, object> healthData, List<string> issues)
    {
        try
        {
            // 检查标准DbContext (Scoped)
            using var scope = _serviceProvider.CreateScope();
            var scopedDbContext = scope.ServiceProvider.GetService<EapGatewayDbContext>();

            if (scopedDbContext == null)
            {
                issues.Add("标准DbContext (Scoped) 未正确注册");
            }
            else
            {
                // 测试数据库连接
                var canConnect = await scopedDbContext.Database.CanConnectAsync();
                healthData["DbContext.CanConnect"] = canConnect;

                if (!canConnect)
                {
                    issues.Add("DbContext无法连接到数据库");
                }
            }

            // 检查DbContextFactory (Singleton)
            var dbContextFactory = _serviceProvider.GetService<IDbContextFactory<EapGatewayDbContext>>();

            if (dbContextFactory == null)
            {
                issues.Add("DbContextFactory未正确注册");
            }
            else
            {
                // 测试工厂创建DbContext
                await using var factoryDbContext = await dbContextFactory.CreateDbContextAsync();
                var factoryCanConnect = await factoryDbContext.Database.CanConnectAsync();
                healthData["DbContextFactory.CanConnect"] = factoryCanConnect;

                if (!factoryCanConnect)
                {
                    issues.Add("DbContextFactory创建的DbContext无法连接数据库");
                }
            }

            healthData["DbContext.ConfigurationStatus"] = "OK";
        }
        catch (Exception ex)
        {
            issues.Add($"DbContext配置检查异常: {ex.Message}");
            healthData["DbContext.ConfigurationStatus"] = "ERROR";
        }
    }

    /// <summary>
    /// 检查仓储工厂模式
    /// </summary>
    private async Task CheckRepositoryFactoryPatternAsync(Dictionary<string, object> healthData, List<string> issues)
    {
        try
        {
            var repositoryFactory = _serviceProvider.GetService<IRepositoryFactory>();

            if (repositoryFactory == null)
            {
                issues.Add("IRepositoryFactory未正确注册");
                healthData["RepositoryFactory.Status"] = "MISSING";
                return;
            }

            // 测试仓储工厂是否能正确创建作用域
            var testResult = await repositoryFactory.ExecuteAsync(async scope =>
            {
                var equipmentRepo = scope.ServiceProvider.GetRequiredService<Core.Repositories.IEquipmentRepository>();
                return equipmentRepo != null;
            });

            if (!testResult)
            {
                issues.Add("RepositoryFactory无法正确创建仓储实例");
            }

            healthData["RepositoryFactory.Status"] = testResult ? "OK" : "ERROR";
        }
        catch (Exception ex)
        {
            issues.Add($"仓储工厂模式检查异常: {ex.Message}");
            healthData["RepositoryFactory.Status"] = "ERROR";
        }
    }

    /// <summary>
    /// 检查SECS/GEM服务配置
    /// </summary>
    private void CheckSecsGemServicesConfiguration(Dictionary<string, object> healthData, List<string> issues)
    {
        try
        {
            // 检查设备服务工厂 (Singleton)
            var deviceServiceFactory = _serviceProvider.GetService<ISecsDeviceServiceFactory>();
            if (deviceServiceFactory == null)
            {
                issues.Add("ISecsDeviceServiceFactory未正确注册为Singleton");
            }

            // 检查设备管理器 (Singleton)
            var deviceManager = _serviceProvider.GetService<ISecsDeviceManager>();
            if (deviceManager == null)
            {
                issues.Add("ISecsDeviceManager未正确注册为Singleton");
            }

            // 检查设备服务的生命周期
            CheckServiceLifetime<ISecsDeviceService>(ServiceLifetime.Transient, healthData, issues, "SecsDeviceService");
            CheckServiceLifetime<IHsmsClient>(ServiceLifetime.Transient, healthData, issues, "HsmsClient");

            healthData["SecsGem.ConfigurationStatus"] = issues.Any(i => i.Contains("SecsGem") || i.Contains("SECS")) ? "ERROR" : "OK";
        }
        catch (Exception ex)
        {
            issues.Add($"SECS/GEM服务配置检查异常: {ex.Message}");
            healthData["SecsGem.ConfigurationStatus"] = "ERROR";
        }
    }

    /// <summary>
    /// 检查连接管理器配置
    /// </summary>
    private void CheckConnectionManagerConfiguration(Dictionary<string, object> healthData, List<string> issues)
    {
        try
        {
            // 检查连接管理器 (Singleton)
            var connectionManager = _serviceProvider.GetService<IMultiDicingMachineConnectionManager>();
            if (connectionManager == null)
            {
                issues.Add("IMultiDicingMachineConnectionManager未正确注册为Singleton");
            }

            // 检查是否与HostedService分离
            var hostedServices = _serviceProvider.GetServices<Microsoft.Extensions.Hosting.IHostedService>();
            var connectionHostedService = hostedServices.FirstOrDefault(s =>
                s.GetType().Name.Contains("DicingMachineConnection"));

            if (connectionHostedService == null)
            {
                issues.Add("DicingMachineConnectionHostedService未正确注册");
            }

            healthData["ConnectionManager.ConfigurationStatus"] =
                issues.Any(i => i.Contains("ConnectionManager") || i.Contains("DicingMachine")) ? "ERROR" : "OK";
        }
        catch (Exception ex)
        {
            issues.Add($"连接管理器配置检查异常: {ex.Message}");
            healthData["ConnectionManager.ConfigurationStatus"] = "ERROR";
        }
    }

    /// <summary>
    /// 检查后台服务配置
    /// </summary>
    private void CheckHostedServicesConfiguration(Dictionary<string, object> healthData, List<string> issues)
    {
        try
        {
            var hostedServices = _serviceProvider.GetServices<Microsoft.Extensions.Hosting.IHostedService>().ToList();
            healthData["HostedServices.Count"] = hostedServices.Count;

            // 检查关键后台服务是否注册
            var expectedHostedServices = new[]
            {
                "DicingMachineConnectionHostedService",
                "DeviceMonitoringHostedService",
                "KafkaProducerMaintenanceHostedService"
            };

            foreach (var expectedService in expectedHostedServices)
            {
                var isRegistered = hostedServices.Any(s => s.GetType().Name.Contains(expectedService.Replace("HostedService", "")));
                healthData[$"HostedService.{expectedService}"] = isRegistered;

                if (!isRegistered)
                {
                    // 有些后台服务可能是可选的，只记录警告
                    _logger.LogWarning("后台服务 {ServiceName} 未注册", expectedService);
                }
            }

            healthData["HostedServices.ConfigurationStatus"] = "OK";
        }
        catch (Exception ex)
        {
            issues.Add($"后台服务配置检查异常: {ex.Message}");
            healthData["HostedServices.ConfigurationStatus"] = "ERROR";
        }
    }

    /// <summary>
    /// 检查缓存服务配置
    /// </summary>
    private void CheckCacheServicesConfiguration(Dictionary<string, object> healthData, List<string> issues)
    {
        try
        {
            // 检查Redis连接 (Singleton)
            var redisConnection = _serviceProvider.GetService<StackExchange.Redis.IConnectionMultiplexer>();
            if (redisConnection != null)
            {
                healthData["Redis.IsConnected"] = redisConnection.IsConnected;

                if (!redisConnection.IsConnected)
                {
                    issues.Add("Redis连接不可用");
                }
            }
            else
            {
                // Redis可能是可选的，使用内存缓存
                healthData["Redis.Status"] = "Not Configured";
            }

            // 检查分布式缓存
            var distributedCache = _serviceProvider.GetService<Microsoft.Extensions.Caching.Distributed.IDistributedCache>();
            if (distributedCache == null)
            {
                issues.Add("IDistributedCache未正确注册");
            }

            // 检查内存缓存
            var memoryCache = _serviceProvider.GetService<Microsoft.Extensions.Caching.Memory.IMemoryCache>();
            if (memoryCache == null)
            {
                issues.Add("IMemoryCache未正确注册");
            }

            healthData["Cache.ConfigurationStatus"] =
                issues.Any(i => i.Contains("Cache") || i.Contains("Redis")) ? "ERROR" : "OK";
        }
        catch (Exception ex)
        {
            issues.Add($"缓存服务配置检查异常: {ex.Message}");
            healthData["Cache.ConfigurationStatus"] = "ERROR";
        }
    }

    /// <summary>
    /// 检查服务生命周期
    /// </summary>
    private void CheckServiceLifetime<T>(ServiceLifetime expectedLifetime, Dictionary<string, object> healthData,
        List<string> issues, string serviceName) where T : class
    {
        try
        {
            var serviceDescriptor = _serviceProvider.GetRequiredService<IServiceCollection>()
                .FirstOrDefault(d => d.ServiceType == typeof(T));

            if (serviceDescriptor == null)
            {
                issues.Add($"{serviceName} 服务未注册");
                return;
            }

            if (serviceDescriptor.Lifetime != expectedLifetime)
            {
                issues.Add($"{serviceName} 生命周期不正确，期望: {expectedLifetime}, 实际: {serviceDescriptor.Lifetime}");
            }

            healthData[$"Service.{serviceName}.Lifetime"] = serviceDescriptor.Lifetime.ToString();
        }
        catch (Exception ex)
        {
            issues.Add($"{serviceName} 生命周期检查异常: {ex.Message}");
        }
    }
}
