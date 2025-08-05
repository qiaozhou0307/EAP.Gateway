using EAP.Gateway.Infrastructure.Persistence.Contexts;

namespace EAP.Gateway.Infrastructure.Persistence.Factories;

/// <summary>
/// DbContext作用域工厂接口
/// </summary>
public interface IDbContextScopeFactory
{
    Task<T> ExecuteAsync<T>(Func<EapGatewayDbContext, Task<T>> operation);
    Task ExecuteAsync(Func<EapGatewayDbContext, Task> operation);
    Task<T> ExecuteInTransactionAsync<T>(Func<EapGatewayDbContext, Task<T>> operation);
}
