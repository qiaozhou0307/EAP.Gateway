using System.ComponentModel.DataAnnotations;

namespace EAP.Gateway.Core.Models;

/// <summary>
/// 裂片机配置选项
/// </summary>
public class DicingMachinesOptions
{
    public const string SectionName = "DicingMachines";

    /// <summary>
    /// 设备配置列表
    /// </summary>
    [Required]
    public DicingMachineConfig[] Devices { get; set; } = Array.Empty<DicingMachineConfig>();

    /// <summary>
    /// 全局设置
    /// </summary>
    public GlobalDicingMachineSettings GlobalSettings { get; set; } = new();

    /// <summary>
    /// 验证配置有效性
    /// </summary>
    public (bool IsValid, List<string> Errors) Validate()
    {
        var errors = new List<string>();

        if (Devices == null || Devices.Length == 0)
        {
            errors.Add("至少需要配置一台裂片机设备");
        }
        else
        {
            for (int i = 0; i < Devices.Length; i++)
            {
                var (isValid, deviceErrors) = Devices[i].Validate();
                if (!isValid)
                {
                    errors.AddRange(deviceErrors.Select(e => $"设备[{i}] {Devices[i].Name}: {e}"));
                }
            }

            // 检查IP地址唯一性
            var duplicateIps = Devices
                .GroupBy(d => $"{d.IpAddress}:{d.Port}")
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);

            foreach (var duplicateIp in duplicateIps)
            {
                errors.Add($"重复的IP地址和端口: {duplicateIp}");
            }

            // 检查编号唯一性
            var duplicateNumbers = Devices
                .Where(d => !string.IsNullOrWhiteSpace(d.ExpectedMachineNumber))
                .GroupBy(d => d.ExpectedMachineNumber)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);

            foreach (var duplicateNumber in duplicateNumbers)
            {
                errors.Add($"重复的裂片机编号: {duplicateNumber}");
            }
        }

        var (globalValid, globalErrors) = GlobalSettings.Validate();
        if (!globalValid)
        {
            errors.AddRange(globalErrors.Select(e => $"全局设置: {e}"));
        }

        return (errors.Count == 0, errors);
    }
}

/// <summary>
/// 全局裂片机设置
/// </summary>
public class GlobalDicingMachineSettings
{
    /// <summary>
    /// 最大并发连接数
    /// </summary>
    [Range(1, 20)]
    public int MaxConcurrentConnections { get; set; } = 5;

    /// <summary>
    /// 默认连接超时时间
    /// </summary>
    public TimeSpan DefaultConnectionTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// 启用自动重连
    /// </summary>
    public bool AutoReconnectEnabled { get; set; } = true;

    /// <summary>
    /// 监控间隔
    /// </summary>
    public TimeSpan MonitoringInterval { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// 健康检查间隔
    /// </summary>
    public TimeSpan HealthCheckInterval { get; set; } = TimeSpan.FromMinutes(2);

    /// <summary>
    /// 验证设置有效性
    /// </summary>
    public (bool IsValid, List<string> Errors) Validate()
    {
        var errors = new List<string>();

        if (MaxConcurrentConnections <= 0 || MaxConcurrentConnections > 20)
        {
            errors.Add("最大并发连接数必须在1-20范围内");
        }

        if (DefaultConnectionTimeout.TotalSeconds < 5 || DefaultConnectionTimeout.TotalSeconds > 300)
        {
            errors.Add("默认连接超时时间必须在5-300秒范围内");
        }

        if (MonitoringInterval.TotalSeconds < 30)
        {
            errors.Add("监控间隔不能小于30秒");
        }

        if (HealthCheckInterval.TotalSeconds < 60)
        {
            errors.Add("健康检查间隔不能小于60秒");
        }

        return (errors.Count == 0, errors);
    }
}
