using EAP.Gateway.Infrastructure.Persistence.Factories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EAP.Gateway.Application.Behaviors;

/// <summary>
/// 事务行为管道 - 为需要事务的命令提供事务支持
/// </summary>
public class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IDbContextScopeFactory _dbContextFactory;
    private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger;

    public TransactionBehavior(
        IDbContextScopeFactory dbContextFactory,
        ILogger<TransactionBehavior<TRequest, TResponse>> logger)
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        // 只有标记了需要事务的请求才使用事务
        if (!typeof(TRequest).GetInterfaces().Any(i => i == typeof(ITransactionalRequest)))
        {
            return await next();
        }

        var requestName = typeof(TRequest).Name;
        _logger.LogInformation("开始事务处理: {RequestName}", requestName);

        try
        {
            return await _dbContextFactory.ExecuteInTransactionAsync(async context =>
            {
                var response = await next();
                _logger.LogInformation("事务处理成功: {RequestName}", requestName);
                return response;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "事务处理失败: {RequestName}", requestName);
            throw;
        }
    }
}
