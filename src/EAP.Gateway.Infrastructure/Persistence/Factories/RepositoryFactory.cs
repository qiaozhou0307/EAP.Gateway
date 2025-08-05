using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using EAP.Gateway.Core.Repositories;
using EAP.Gateway.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EAP.Gateway.Infrastructure.Persistence.Factories;

/// <summary>
/// 仓储工厂实现 - 为单例服务提供Scoped仓储访问
/// </summary>
public class RepositoryFactory : IRepositoryFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RepositoryFactory> _logger;

    public RepositoryFactory(IServiceProvider serviceProvider, ILogger<RepositoryFactory> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 执行需要仓储访问的操作并返回结果
    /// </summary>
    public async Task<T> ExecuteAsync<T>(Func<IServiceScope, Task<T>> operation)
    {
        if (operation == null)
            throw new ArgumentNullException(nameof(operation));

        using var scope = _serviceProvider.CreateScope();

        try
        {
            return await operation(scope);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行仓储操作时发生异常");
            throw;
        }
    }

    /// <summary>
    /// 执行需要仓储访问的操作
    /// </summary>
    public async Task ExecuteAsync(Func<IServiceScope, Task> operation)
    {
        if (operation == null)
            throw new ArgumentNullException(nameof(operation));

        using var scope = _serviceProvider.CreateScope();

        try
        {
            await operation(scope);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行仓储操作时发生异常");
            throw;
        }
    }

    /// <summary>
    /// 获取特定类型的仓储实例
    /// </summary>
    public async Task<T> GetRepositoryAsync<T>() where T : class
    {
        return await ExecuteAsync<T>(scope =>
        {
            var repository = scope.ServiceProvider.GetRequiredService<T>();
            return Task.FromResult(repository);
        });
    }

    /// <summary>
    /// 在DbContext作用域内执行操作
    /// </summary>
    public async Task<T> ExecuteInScopeAsync<T>(Func<IEquipmentRepository, IAlarmRepository, IDataVariableRepository, Task<T>> operation)
    {
        if (operation == null)
            throw new ArgumentNullException(nameof(operation));

        return await ExecuteAsync(async scope =>
        {
            var equipmentRepo = scope.ServiceProvider.GetRequiredService<IEquipmentRepository>();
            var alarmRepo = scope.ServiceProvider.GetRequiredService<IAlarmRepository>();
            var dataVariableRepo = scope.ServiceProvider.GetRequiredService<IDataVariableRepository>();

            return await operation(equipmentRepo, alarmRepo, dataVariableRepo);
        });
    }
}
