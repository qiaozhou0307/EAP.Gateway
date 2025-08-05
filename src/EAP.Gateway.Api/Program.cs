using EAP.Gateway.Api.Extensions;
using EAP.Gateway.Api.Middleware;
using EAP.Gateway.Api.ModelBinders;
using EAP.Gateway.Application;
using EAP.Gateway.Infrastructure;
using EAP.Gateway.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// 配置Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

try
{
    Log.Information("启动 EAP Gateway API 服务配置");

    // 添加控制器和模型绑定器
    builder.Services.AddControllers(options =>
    {
        options.ModelBinderProviders.Insert(0, new EquipmentIdModelBinderProvider());
    });

    // API文档
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // 自定义健康检查
    builder.Services.AddCustomHealthChecks(builder.Configuration);

    // CORS
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
    });

    // 修复：添加应用层和基础设施层服务
    builder.Services.AddApplication();
    //builder.Services.AddInfrastructure(builder.Configuration); // 使用兼容方法
    builder.Services.AddInfrastructureServices(builder.Configuration, builder.Environment);

    var app = builder.Build();

    // 配置HTTP请求管道
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // 全局异常处理中间件
    app.UseMiddleware<GlobalExceptionMiddleware>();

    app.UseCors("AllowAll");
    app.UseRouting();
    app.MapControllers();
    app.MapHealthChecks("/health");

    // 数据库迁移（开发环境）
    if (app.Environment.IsDevelopment())
    {
        using var scope = app.Services.CreateScope();
        try
        {
            var context = scope.ServiceProvider.GetRequiredService<EapGatewayDbContext>();
            await context.Database.MigrateAsync();
            Log.Information("数据库迁移完成");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "数据库迁移失败");
        }
    }

    Log.Information("启动 EAP Gateway API 服务");
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "EAP Gateway API 启动失败");
}
finally
{
    Log.CloseAndFlush();
}
