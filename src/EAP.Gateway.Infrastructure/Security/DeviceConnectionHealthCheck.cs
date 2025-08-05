using EAP.Gateway.Core.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace EAP.Gateway.Infrastructure.HealthChecks;

/// <summary>
/// 设备连接健康检查
/// </summary>
public class DeviceConnectionHealthCheck : IHealthCheck
{
    private readonly IMultiDicingMachineConnectionManager _connectionManager;
    private readonly ILogger<DeviceConnectionHealthCheck> _logger;

    public DeviceConnectionHealthCheck(
        IMultiDicingMachineConnectionManager connectionManager,
        ILogger<DeviceConnectionHealthCheck> logger)
    {
        _connectionManager = connectionManager;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var healthData = new Dictionary<string, object>();

        try
        {
            var deviceStatuses = await _connectionManager.GetAllDicingMachineStatusAsync();
            var totalDevices = deviceStatuses.Count();
            var healthyDevices = deviceStatuses.Count(s => s.IsHealthy);
            var unhealthyDevices = totalDevices - healthyDevices;

            healthData["TotalDevices"] = totalDevices;
            healthData["HealthyDevices"] = healthyDevices;
            healthData["UnhealthyDevices"] = unhealthyDevices;

            var statistics = _connectionManager.GetConnectionStatistics();
            healthData["ConnectionAttempts"] = statistics.TotalConnectionAttempts;
            healthData["SuccessfulConnections"] = statistics.SuccessfulConnections;
            healthData["FailedConnections"] = statistics.FailedConnections;
            healthData["SuccessRate"] = statistics.TotalConnectionAttempts > 0
                ? (double)statistics.SuccessfulConnections / statistics.TotalConnectionAttempts * 100
                : 0;

            if (totalDevices == 0)
            {
                return HealthCheckResult.Healthy("无设备连接", healthData);
            }

            if (unhealthyDevices == 0)
            {
                return HealthCheckResult.Healthy($"所有 {totalDevices} 台设备连接正常", healthData);
            }

            if (unhealthyDevices < totalDevices)
            {
                return HealthCheckResult.Degraded(
                    $"{unhealthyDevices} 台设备连接异常，{healthyDevices} 台设备正常",
                    data: healthData);
            }

            return HealthCheckResult.Unhealthy("所有设备连接异常", data: healthData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "设备连接健康检查异常");
            return HealthCheckResult.Unhealthy("设备连接健康检查失败", ex, healthData);
        }
    }
}
