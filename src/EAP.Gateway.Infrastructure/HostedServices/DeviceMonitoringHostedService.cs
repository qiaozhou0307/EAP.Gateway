using EAP.Gateway.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EAP.Gateway.Infrastructure.HostedServices;

/// <summary>
/// 设备监控后台服务 - 定期检查设备健康状态
/// </summary>
public class DeviceMonitoringHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DeviceMonitoringHostedService> _logger;
    private readonly DeviceMonitoringOptions _options;

    public DeviceMonitoringHostedService(
        IServiceProvider serviceProvider,
        IOptions<DeviceMonitoringOptions> options,
        ILogger<DeviceMonitoringHostedService> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("设备监控后台服务已启动，监控间隔: {Interval}秒", _options.MonitoringIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformHealthCheckAsync();
                await Task.Delay(TimeSpan.FromSeconds(_options.MonitoringIntervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // 服务正在停止
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "设备健康检查过程中发生异常");

                // 异常情况下等待一段时间再重试
                await Task.Delay(TimeSpan.FromSeconds(_options.ErrorRetryDelaySeconds), stoppingToken);
            }
        }

        _logger.LogInformation("设备监控后台服务已停止");
    }

    private async Task PerformHealthCheckAsync()
    {
        using var scope = _serviceProvider.CreateScope();

        try
        {
            var connectionManager = scope.ServiceProvider.GetService<IMultiDicingMachineConnectionManager>();
            if (connectionManager == null)
            {
                _logger.LogWarning("未找到设备连接管理器，跳过健康检查");
                return;
            }

            var deviceStatuses = await connectionManager.GetAllDicingMachineStatusAsync();
            var totalDevices = deviceStatuses.Count();
            var healthyDevices = deviceStatuses.Count(s => s.IsHealthy);
            var unhealthyDevices = totalDevices - healthyDevices;

            _logger.LogDebug("设备健康检查完成 - 总设备: {Total}, 健康: {Healthy}, 异常: {Unhealthy}",
                totalDevices, healthyDevices, unhealthyDevices);

            // 如果有异常设备，记录警告
            if (unhealthyDevices > 0)
            {
                var unhealthyDeviceIds = deviceStatuses
                    .Where(s => !s.IsHealthy)
                    .Select(s => s.DeviceId)
                    .ToList();

                _logger.LogWarning("发现 {Count} 台异常设备: {DeviceIds}",
                    unhealthyDevices, string.Join(", ", unhealthyDeviceIds));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行设备健康检查时发生异常");
        }
    }
}
