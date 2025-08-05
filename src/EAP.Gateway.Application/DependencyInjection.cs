using Microsoft.Extensions.DependencyInjection;
using MediatR;
using FluentValidation;
using AutoMapper;
using EAP.Gateway.Application.Behaviors;
using EAP.Gateway.Application.Services;
using System.Reflection;

namespace EAP.Gateway.Application;

/// <summary>
/// 应用层依赖注入配置 - 修复MediatR 12.x配置问题
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

            // 注册行为管道（MediatR 12.x 的正确方式）
            cfg.AddBehavior<ValidationBehavior<,>>();
            cfg.AddBehavior<LoggingBehavior<,>>();
            cfg.AddBehavior<PerformanceBehavior<,>>();
            cfg.AddBehavior<TransactionBehavior<,>>();
        });

        // FluentValidation - 注册验证器
        services.AddValidatorsFromAssembly(assembly, ServiceLifetime.Scoped);

        // AutoMapper - 注册映射配置
        services.AddAutoMapper(cfg =>
        {
            cfg.AddProfile<EquipmentMappingProfile>();
            cfg.AddProfile<AlarmMappingProfile>();
            cfg.AddProfile<DataVariableMappingProfile>();
            cfg.AddProfile<MessageMappingProfile>();
        });

        // 应用服务注册看
        AddApplicationServices(services);

        return services;
    }

    /// <summary>
    /// 添加应用服务
    /// </summary>
    private static void AddApplicationServices(IServiceCollection services)
    {
        // 设备管理应用服务
        services.AddScoped<IEquipmentApplicationService, EquipmentApplicationService>();
        services.AddScoped<IDeviceConnectionApplicationService, DeviceConnectionApplicationService>();

        // 数据采集应用服务
        services.AddScoped<IDataCollectionApplicationService, DataCollectionApplicationService>();
        services.AddScoped<IRealtimeDataApplicationService, RealtimeDataApplicationService>();

        // 报警管理应用服务
        services.AddScoped<IAlarmApplicationService, AlarmApplicationService>();

        // 配方管理应用服务
        services.AddScoped<IRecipeApplicationService, RecipeApplicationService>();

        // 命令执行应用服务
        services.AddScoped<IRemoteCommandApplicationService, RemoteCommandApplicationService>();

        // 查询服务（可以注册为单例，因为是只读的）
        services.AddSingleton<IEquipmentQueryService, EquipmentQueryService>();
        services.AddSingleton<IAlarmQueryService, AlarmQueryService>();
        services.AddSingleton<IDataVariableQueryService, DataVariableQueryService>();
    }
}
