using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using EAP.Gateway.Core.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace EAP.Gateway.Infrastructure.Persistence.Factories;

/// <summary>
/// 仓储工厂接口 - 解决单例服务访问Scoped仓储的生命周期问题
/// </summary>
public interface IRepositoryFactory
{
    /// <summary>
    /// 执行需要仓储访问的操作并返回结果
    /// </summary>
    Task<T> ExecuteAsync<T>(Func<IServiceScope, Task<T>> operation);

    /// <summary>
    /// 执行需要仓储访问的操作
    /// </summary>
    Task ExecuteAsync(Func<IServiceScope, Task> operation);

    /// <summary>
    /// 获取特定类型的仓储实例
    /// </summary>
    Task<T> GetRepositoryAsync<T>() where T : class;

    /// <summary>
    /// 在DbContext作用域内执行操作
    /// </summary>
    Task<T> ExecuteInScopeAsync<T>(Func<IEquipmentRepository, IAlarmRepository, IDataVariableRepository, Task<T>> operation);
}
