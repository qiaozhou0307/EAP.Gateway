using EAP.Gateway.Application;
using EAP.Gateway.Infrastructure;
using EAP.Gateway.Worker.Services;
using Serilog;
using Serilog.Events;
using System.Reflection;

// Configure Serilog early
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/eap-gateway-.log",
        rollingInterval: RollingInterval.Day,
        rollOnFileSizeLimit: true,
        fileSizeLimitBytes: 52428800, // 50MB
        retainedFileCountLimit: 7)
    .CreateLogger();

try
{
    Log.Information("🚀 Starting EAP Gateway Worker Service with Dicing Machine Support...");
    Log.Information("Version: {Version}", Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown");

    var builder = Host.CreateApplicationBuilder(args);

    // Configure Serilog
    builder.Services.AddSerilog();

    // Configure as Windows Service if needed
    if (OperatingSystem.IsWindows())
    {
        builder.Services.AddWindowsService(options =>
        {
            options.ServiceName = "EAP.Gateway.Worker";
        });
    }

    // Add health checks
    builder.Services.AddHealthChecks();

    // Add application layers
    builder.Services.AddApplication(); // 使用正确的扩展方法名
    builder.Services.AddInfrastructureServices(builder.Configuration);

    // Add dicing machine services
    builder.Services.AddDicingMachineServices(builder.Configuration);

    // Register the worker service (关键修复)
    builder.Services.AddHostedService<DicingMachineConnectionWorker>();

    var host = builder.Build();

    Log.Information("✅ EAP Gateway Worker Service started successfully");

    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "❌ EAP Gateway Worker Service failed to start");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}

return 0;
