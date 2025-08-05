using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace EAP.Gateway.Application.Behaviors;

/// <summary>
/// 日志记录行为管道
/// 记录命令/查询的执行时间和结果
/// </summary>
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("开始执行 {RequestName}", requestName);

        try
        {
            var response = await next();
            stopwatch.Stop();

            _logger.LogInformation("完成执行 {RequestName}, 耗时: {ElapsedMilliseconds}ms",
                requestName, stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "执行 {RequestName} 失败, 耗时: {ElapsedMilliseconds}ms",
                requestName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
