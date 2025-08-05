using Microsoft.Extensions.DependencyInjection;
using MediatR;
using FluentValidation;
using AutoMapper;
using EAP.Gateway.Application.Behaviors;
using System.Reflection;

namespace EAP.Gateway.Application;

/// <summary>
/// 应用层依赖注入配置 - MediatR 12.x 版本
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // MediatR 12.x 正确配置
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
        });

        // 行为管道需要单独注册（MediatR 12.x 的正确方式）
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

        // FluentValidation
        services.AddValidatorsFromAssembly(assembly);

        // AutoMapper
        services.AddAutoMapper(typeof(EquipmentMappingProfile));

        return services;
    }
}
