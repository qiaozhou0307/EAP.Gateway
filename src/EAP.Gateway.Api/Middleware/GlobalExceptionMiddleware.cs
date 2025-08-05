using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using EAP.Gateway.Core.Exceptions;

namespace EAP.Gateway.Api.Middleware;

/// <summary>
/// 全局异常处理中间件（修复版本）
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发生未处理的异常");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        // 修复：重新组织switch表达式，避免模式冲突
        var response = exception switch
        {
            // 最具体的异常类型放在前面
            EquipmentNotFoundException notFoundEx => new ErrorResponse
            {
                Message = notFoundEx.Message,
                StatusCode = (int)HttpStatusCode.NotFound,
                Type = "EquipmentNotFound",
                Details = new { EquipmentId = notFoundEx.EquipmentId.Value }
            },
            // 然后是基类异常
            DomainException domainEx => new ErrorResponse
            {
                Message = domainEx.Message,
                StatusCode = (int)HttpStatusCode.BadRequest,
                Type = "DomainError",
                Details = new { ExceptionType = domainEx.GetType().Name }
            },
            // 参数异常
            ArgumentNullException argNullEx => new ErrorResponse
            {
                Message = $"参数不能为空: {argNullEx.ParamName}",
                StatusCode = (int)HttpStatusCode.BadRequest,
                Type = "ArgumentNull",
                Details = new { ParameterName = argNullEx.ParamName }
            },
            ArgumentException argEx => new ErrorResponse
            {
                Message = argEx.Message,
                StatusCode = (int)HttpStatusCode.BadRequest,
                Type = "ArgumentError",
                Details = new { ParameterName = argEx.ParamName }
            },
            // 操作取消异常
            OperationCanceledException => new ErrorResponse
            {
                Message = "操作已被取消",
                StatusCode = (int)HttpStatusCode.BadRequest,
                Type = "OperationCanceled"
            },
            // 超时异常
            TimeoutException timeoutEx => new ErrorResponse
            {
                Message = "操作超时",
                StatusCode = (int)HttpStatusCode.RequestTimeout,
                Type = "Timeout",
                Details = new { Message = timeoutEx.Message }
            },
            // 未授权异常
            UnauthorizedAccessException => new ErrorResponse
            {
                Message = "访问被拒绝",
                StatusCode = (int)HttpStatusCode.Unauthorized,
                Type = "Unauthorized"
            },
            // 无效操作异常
            InvalidOperationException invalidOpEx => new ErrorResponse
            {
                Message = invalidOpEx.Message,
                StatusCode = (int)HttpStatusCode.BadRequest,
                Type = "InvalidOperation"
            },
            // 不支持的操作异常
            NotSupportedException notSupportedEx => new ErrorResponse
            {
                Message = notSupportedEx.Message,
                StatusCode = (int)HttpStatusCode.NotImplemented,
                Type = "NotSupported"
            },
            // 默认情况 - 内部服务器错误
            _ => new ErrorResponse
            {
                Message = "服务器内部错误",
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Type = "InternalError",
                Details = new { ExceptionType = exception.GetType().Name }
            }
        };

        context.Response.StatusCode = response.StatusCode;

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        var jsonResponse = JsonSerializer.Serialize(response, jsonOptions);
        await context.Response.WriteAsync(jsonResponse);
    }

    /// <summary>
    /// 错误响应模型
    /// </summary>
    private sealed class ErrorResponse
    {
        /// <summary>
        /// 错误消息
        /// </summary>
        public required string Message { get; set; }

        /// <summary>
        /// HTTP状态码
        /// </summary>
        public required int StatusCode { get; set; }

        /// <summary>
        /// 错误类型
        /// </summary>
        public required string Type { get; set; }

        /// <summary>
        /// 错误详情
        /// </summary>
        public object? Details { get; set; }

        /// <summary>
        /// 时间戳
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 跟踪ID
        /// </summary>
        public string TraceId { get; set; } = Guid.NewGuid().ToString("N")[..8];
    }
}
