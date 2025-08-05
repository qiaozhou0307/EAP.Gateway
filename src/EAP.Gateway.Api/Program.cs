using EAP.Gateway.Application;
using EAP.Gateway.Infrastructure;
using EAP.Gateway.Infrastructure.Configuration;
using EAP.Gateway.Infrastructure.HostedServices;
using EAP.Gateway.Infrastructure.Middleware;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Secs4Net;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// 配置Serilog
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// 添加服务到容器
ConfigureServices(builder.Services, builder.Configuration, builder.Environment);

var app = builder.Build();

// 配置HTTP请求管道
ConfigurePipeline(app);

await app.RunAsync();

// 配置服务注册 - 修复生命周期冲突
static void ConfigureServices(IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
{
    // 1. 配置选项
    ConfigureOptions(services, configuration);

    // 2. 核心层服务（无依赖）
    // Core层通常不需要DI注册，因为它主要包含领域对象

    // 3. 应用层服务（依赖Core层）
    services.AddApplication();

    // 4. 基础设施层服务（依赖Core和Application层）
    services.AddInfrastructureServices(configuration, environment);

    // 5. API层服务
    ConfigureApiServices(services, configuration);

    // 6. 后台服务（必须在最后注册）
    ConfigureHostedServices(services, configuration);
}

// 配置选项绑定
static void ConfigureOptions(IServiceCollection services, IConfiguration configuration)
{
    // 基础设施配置
    services.Configure<ConnectionManagerOptions>(configuration.GetSection("ConnectionManager"));
    services.Configure<SecsGemOptions>(configuration.GetSection("SecsGem"));
    services.Configure<KafkaConfig>(configuration.GetSection("Kafka"));
    services.Configure<RabbitMQConfig>(configuration.GetSection("RabbitMQ"));
    services.Configure<DeviceMonitoringOptions>(configuration.GetSection(DeviceMonitoringOptions.SectionName));

    // API配置
    services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
    services.Configure<CorsOptions>(configuration.GetSection("Cors"));
}


// 配置API服务
static void ConfigureApiServices(IServiceCollection services, IConfiguration configuration)
{
    // Controllers和API Explorer
    services.AddControllers();
    services.AddEndpointsApiExplorer();

    // Swagger/OpenAPI
    services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() { Title = "EAP Gateway API", Version = "v1" });

        // 添加JWT认证支持
        c.AddSecurityDefinition("Bearer", new()
        {
            Description = "JWT Authorization header using the Bearer scheme.",
            Name = "Authorization",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });
    });

    // CORS配置
    services.AddCors(options =>
    {
        options.AddPolicy("AllowSpecificOrigins", policy =>
        {
            var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        });
    });

    // 认证和授权
    ConfigureAuthentication(services, configuration);

    // gRPC服务
    services.AddGrpc();

    // 压缩
    services.AddResponseCompression(options =>
    {
        options.EnableForHttps = true;
    });

    // API版本控制
    services.AddApiVersioning(opt =>
    {
        opt.DefaultApiVersion = new Microsoft.AspNetCore.Mvc.ApiVersion(1, 0);
        opt.AssumeDefaultVersionWhenUnspecified = true;
        opt.ApiVersionReader = Microsoft.AspNetCore.Mvc.ApiVersionReader.Combine(
            new Microsoft.AspNetCore.Mvc.QueryStringApiVersionReader("apiVersion"),
            new Microsoft.AspNetCore.Mvc.HeaderApiVersionReader("X-Version"),
            new Microsoft.AspNetCore.Mvc.UrlSegmentApiVersionReader()
        );
    });

    services.AddVersionedApiExplorer(setup =>
    {
        setup.GroupNameFormat = "'v'VVV";
        setup.SubstituteApiVersionInUrl = true;
    });
}


// 配置认证服务
static void ConfigureAuthentication(IServiceCollection services, IConfiguration configuration)
{
    var jwtOptions = configuration.GetSection("Jwt").Get<JwtOptions>();
    if (jwtOptions != null && !string.IsNullOrEmpty(jwtOptions.SecretKey))
    {
        services.AddAuthentication("Bearer")
            .AddJwtBearer("Bearer", options =>
            {
                options.TokenValidationParameters = new()
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                        System.Text.Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddAuthorization();
    }
}


// 配置后台服务 - 必须在所有依赖服务注册完成后注册
static void ConfigureHostedServices(IServiceCollection services, IConfiguration configuration)
{
    var deviceMonitoringOptions = configuration.GetSection(DeviceMonitoringOptions.SectionName).Get<DeviceMonitoringOptions>();

    // 设备连接管理后台服务
    services.AddHostedService<DicingMachineConnectionHostedService>();

    // 设备监控后台服务（可选）
    if (deviceMonitoringOptions?.EnableMonitoring == true)
    {
        services.AddHostedService<DeviceMonitoringHostedService>();
    }

    // Kafka维护后台服务
    var kafkaConfig = configuration.GetSection("Kafka").Get<KafkaConfig>();
    if (kafkaConfig != null && !string.IsNullOrEmpty(kafkaConfig.BootstrapServers))
    {
        services.AddHostedService<KafkaProducerMaintenanceHostedService>();
    }
}


// 配置HTTP请求管道
static void ConfigurePipeline(WebApplication app)
{
    // 开发环境配置
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "EAP Gateway API V1"));
        app.UseDeveloperExceptionPage();
    }
    else
    {
        app.UseExceptionHandler("/Error");
        app.UseHsts();
    }

    // 请求日志
    app.UseSerilogRequestLogging();

    // 安全头
    app.UseSecurityHeaders();

    // HTTPS重定向
    app.UseHttpsRedirection();

    // 静态文件
    app.UseStaticFiles();

    // 路由
    app.UseRouting();

    // CORS
    app.UseCors("AllowSpecificOrigins");

    // 认证和授权
    app.UseAuthentication();
    app.UseAuthorization();

    // 响应压缩
    app.UseResponseCompression();

    // 自定义中间件
    app.UseMiddleware<RequestResponseLoggingMiddleware>();
    app.UseMiddleware<ExceptionHandlingMiddleware>();

    // 端点映射
    app.MapControllers();
    app.MapGrpcServices();

    // 健康检查
    app.MapHealthChecks("/health", new HealthCheckOptions()
    {
        Predicate = _ => true,
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });

    app.MapHealthChecks("/health/ready", new HealthCheckOptions()
    {
        Predicate = check => check.Tags.Contains("ready"),
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });

    app.MapHealthChecks("/health/live", new HealthCheckOptions()
    {
        Predicate = _ => false,
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });
}

// 扩展方法
public static class WebApplicationExtensions
{
    public static void MapGrpcServices(this WebApplication app)
    {
        // 映射gRPC服务
        // app.MapGrpcService<EquipmentService>();
        // app.MapGrpcService<DataCollectionService>();
    }
}
