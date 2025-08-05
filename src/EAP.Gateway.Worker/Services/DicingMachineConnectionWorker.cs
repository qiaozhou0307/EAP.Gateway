using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using EAP.Gateway.Core.Models;
using EAP.Gateway.Core.Services;
using EAP.Gateway.Core.ValueObjects;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static EAP.Gateway.Infrastructure.DependencyInjection;

namespace EAP.Gateway.Worker.Services;

/// <summary>
/// 裂片机连接后台工作服务
/// 负责启动时自动连接配置的裂片机，并维护连接状态
/// </summary>
public class DicingMachineConnectionWorker : BackgroundService
{
    private readonly IMultiDicingMachineConnectionManager _connectionManager;
    private readonly IConfiguration _configuration;
    private readonly IOptionsMonitor<DicingMachinesOptions> _options;
    private readonly ILogger<DicingMachineConnectionWorker> _logger;

    public DicingMachineConnectionWorker(
        IMultiDicingMachineConnectionManager connectionManager,
        IConfiguration configuration,
        IOptionsMonitor<DicingMachinesOptions> options,
        ILogger<DicingMachineConnectionWorker> logger)
    {
        _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 裂片机连接工作器启动");

        try
        {
            // 等待系统初始化完成
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

            // 从配置文件加载裂片机配置
            var machineConfigs = LoadDicingMachineConfigurations();

            if (!machineConfigs.Any())
            {
                _logger.LogWarning("⚠️ 未找到裂片机配置，工作器将保持运行状态");
                await KeepRunningAsync(stoppingToken);
                return;
            }

            // 验证所有配置
            var validConfigs = ValidateConfigurations(machineConfigs);

            if (!validConfigs.Any())
            {
                _logger.LogError("❌ 所有裂片机配置验证失败");
                return;
            }

            // 并发连接所有裂片机
            _logger.LogInformation("🔗 开始连接 {Count} 台裂片机", validConfigs.Count);
            var connectionResult = await _connectionManager.ConnectMultipleDicingMachinesAsync(
                validConfigs, maxConcurrency: 3);

            // 记录连接结果
            LogConnectionResults(connectionResult);

            // 如果有成功连接的设备，启动监控循环
            if (connectionResult.SuccessfulCount > 0)
            {
                await StartMonitoringLoopAsync(stoppingToken);
            }
            else
            {
                _logger.LogError("❌ 所有裂片机连接失败，工作器退出");
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("🛑 裂片机连接工作器被取消");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 裂片机连接工作器异常");
        }
        finally
        {
            _logger.LogInformation("🏁 裂片机连接工作器已退出");
        }
    }

    /// <summary>
    /// 从配置文件加载裂片机配置
    /// </summary>
    private List<DicingMachineConfig> LoadDicingMachineConfigurations()
    {
        try
        {
            // 优先使用 IOptionsMonitor
            var optionsValue = _options.CurrentValue;
            if (optionsValue?.Devices != null && optionsValue.Devices.Any())
            {
                _logger.LogInformation("📋 从 Options 加载 {Count} 个裂片机配置", optionsValue.Devices.Length);
                return optionsValue.Devices.ToList();
            }

            // 备用：直接从 IConfiguration 读取
            var configs = _configuration.GetSection("DicingMachines:Devices")
                .Get<DicingMachineConfig[]>() ?? Array.Empty<DicingMachineConfig>();

            _logger.LogInformation("📋 从配置文件加载 {Count} 个裂片机配置", configs.Length);

            foreach (var config in configs)
            {
                _logger.LogDebug("📄 配置详情: {Name} - {IP}:{Port} (期望编号: {ExpectedNumber})",
                    config.Name, config.IpAddress, config.Port, config.ExpectedMachineNumber ?? "自动检测");
            }

            return configs.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 加载裂片机配置失败");
            return new List<DicingMachineConfig>();
        }
    }

    // ... 其他方法实现保持相同 ...

    private List<DicingMachineConfig> ValidateConfigurations(List<DicingMachineConfig> configs)
    {
        var validConfigs = new List<DicingMachineConfig>();

        foreach (var config in configs)
        {
            var (isValid, errors) = config.Validate();

            if (isValid)
            {
                validConfigs.Add(config);
                _logger.LogDebug("✅ 配置验证通过: {Name}", config.Name);
            }
            else
            {
                _logger.LogError("❌ 配置验证失败: {Name}, 错误: {Errors}",
                    config.Name, string.Join(", ", errors));
            }
        }

        return validConfigs;
    }

    private void LogConnectionResults(MultiConnectionResult result)
    {
        _logger.LogInformation("📊 连接结果汇总:");
        _logger.LogInformation("   总数: {Total}", result.TotalCount);
        _logger.LogInformation("   成功: {Success}", result.SuccessfulCount);
        _logger.LogInformation("   失败: {Failed}", result.FailedCount);
        _logger.LogInformation("   成功率: {SuccessRate:F1}%", result.SuccessRate);
        _logger.LogInformation("   总用时: {Duration:F1}秒", result.TotalDuration.TotalSeconds);

        foreach (var success in result.GetSuccessfulConnections())
        {
            _logger.LogInformation("✅ {EquipmentId}: {MachineNumber} v{Version} ({Duration:F1}秒)",
                success.EquipmentId?.Value,
                success.MachineMetadata?.MachineNumber,
                success.MachineMetadata?.Version,
                success.Duration.TotalSeconds);
        }

        foreach (var failure in result.GetFailedConnections())
        {
            _logger.LogError("❌ {IP}:{Port}: {Error}",
                failure.IpAddress, failure.Port, failure.ErrorMessage);
        }
    }

    private async Task StartMonitoringLoopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("👁️ 启动裂片机连接监控循环");

        const int monitoringIntervalMinutes = 1;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(monitoringIntervalMinutes), stoppingToken);

                var deviceStatuses = await _connectionManager.GetAllDicingMachineStatusAsync();
                var statistics = _connectionManager.GetConnectionStatistics();

                LogMonitoringStatistics(statistics, deviceStatuses);
                await CheckAndReconnectDisconnectedDevicesAsync(deviceStatuses);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "监控循环异常");
            }
        }

        _logger.LogInformation("🛑 裂片机连接监控循环已停止");
    }

    private async Task KeepRunningAsync(CancellationToken stoppingToken)
    {
        const int idleCheckIntervalMinutes = 5;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(idleCheckIntervalMinutes), stoppingToken);
                _logger.LogInformation("💤 工作器保持运行状态，等待配置更新");
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private void LogMonitoringStatistics(ConnectionStatistics statistics, IEnumerable<DicingMachineStatus> deviceStatuses)
    {
        _logger.LogInformation("📈 连接统计 [总数: {Total}, 已连接: {Connected}, 在线: {Online}, 连接率: {Rate:F1}%]",
            statistics.TotalDevices, statistics.ConnectedDevices, statistics.OnlineDevices, statistics.ConnectionRate);

        foreach (var status in deviceStatuses)
        {
            var statusIcon = GetStatusIcon(status);
            _logger.LogDebug("{Icon} {MachineNumber}: {Status} (心跳: {Heartbeat})",
                statusIcon, status.MachineNumber, status.GetStatusDescription(),
                status.LastHeartbeat?.ToString("HH:mm:ss") ?? "无");
        }
    }

    private async Task CheckAndReconnectDisconnectedDevicesAsync(IEnumerable<DicingMachineStatus> deviceStatuses)
    {
        var disconnectedDevices = deviceStatuses.Where(s => !s.IsConnected).ToList();

        if (disconnectedDevices.Any())
        {
            _logger.LogWarning("⚠️ 发现 {Count} 台断开连接的裂片机", disconnectedDevices.Count);

            foreach (var device in disconnectedDevices)
            {
                try
                {
                    _logger.LogInformation("🔄 尝试重连裂片机: {MachineNumber}", device.MachineNumber);
                    var success = await _connectionManager.ReconnectDicingMachineAsync(device.MachineNumber);

                    if (success)
                    {
                        _logger.LogInformation("✅ 裂片机重连成功: {MachineNumber}", device.MachineNumber);
                    }
                    else
                    {
                        _logger.LogWarning("❌ 裂片机重连失败: {MachineNumber}", device.MachineNumber);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "重连裂片机异常: {MachineNumber}", device.MachineNumber);
                }
            }
        }
    }

    private string GetStatusIcon(DicingMachineStatus status)
    {
        if (!status.IsConnected) return "🔴";
        if (!status.IsOnline) return "🟡";

        return status.HealthStatus switch
        {
            HealthStatus.Healthy => "🟢",
            HealthStatus.Degraded => "🟡",
            HealthStatus.Unhealthy => "🔴",
            _ => "⚪"
        };
    }
}
