using EAP.Gateway.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EAP.Gateway.Infrastructure.Persistence.Factories;

/// <summary>
/// DbContext作用域工厂 - 为后台服务提供DbContext访问
/// </summary>
public interface IDbContextScopeFactory
{
    Task<T> ExecuteAsync<T>(Func<EapGatewayDbContext, Task<T>> operation);
    Task ExecuteAsync(Func<EapGatewayDbContext, Task> operation);
    Task<T> ExecuteInTransactionAsync<T>(Func<EapGatewayDbContext, Task<T>> operation);
}

public class DbContextScopeFactory : IDbContextScopeFactory
{
    private readonly IDbContextFactory<EapGatewayDbContext> _contextFactory;
    private readonly ILogger<DbContextScopeFactory> _logger;

    public DbContextScopeFactory(
        IDbContextFactory<EapGatewayDbContext> contextFactory,
        ILogger<DbContextScopeFactory> logger)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<T> ExecuteAsync<T>(Func<EapGatewayDbContext, Task<T>> operation)
    {
        if (operation == null)
            throw new ArgumentNullException(nameof(operation));

        await using var context = await _contextFactory.CreateDbContextAsync();

        try
        {
            return await operation(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行DbContext操作时发生异常");
            throw;
        }
    }

    public async Task ExecuteAsync(Func<EapGatewayDbContext, Task> operation)
    {
        if (operation == null)
            throw new ArgumentNullException(nameof(operation));

        await using var context = await _contextFactory.CreateDbContextAsync();

        try
        {
            await operation(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行DbContext操作时发生异常");
            throw;
        }
    }

    public async Task<T> ExecuteInTransactionAsync<T>(Func<EapGatewayDbContext, Task<T>> operation)
    {
        if (operation == null)
            throw new ArgumentNullException(nameof(operation));

        await using var context = await _contextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            var result = await operation(context);
            await transaction.CommitAsync();
            return result;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "执行事务操作时发生异常，已回滚");
            throw;
        }
    }
}
