using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EAP.Gateway.Application.Behaviors;

/// <summary>
/// 验证行为管道 - 在命令/查询执行前进行验证
/// </summary>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    private readonly ILogger<ValidationBehavior<TRequest, TResponse>> _logger;

    public ValidationBehavior(
        IEnumerable<IValidator<TRequest>> validators,
        ILogger<ValidationBehavior<TRequest, TResponse>> logger)
    {
        _validators = validators;
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Any())
        {
            var requestName = typeof(TRequest).Name;
            _logger.LogWarning("验证失败 - 请求: {RequestName}, 错误数量: {ErrorCount}",
                requestName, failures.Count);

            throw new ValidationException(failures);
        }

        return await next();
    }
}
