using EAP.Gateway.Api.Middleware;

namespace EAP.Gateway.Api.Extensions;

/// <summary>
/// 异常处理扩展方法
/// </summary>
public static class ExceptionHandlingExtensions
{
    /// <summary>
    /// 添加全局异常处理中间件
    /// </summary>
    public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder app)
    {
        return app.UseMiddleware<GlobalExceptionMiddleware>();
    }
}
