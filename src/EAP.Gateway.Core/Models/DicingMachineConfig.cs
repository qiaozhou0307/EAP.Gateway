namespace EAP.Gateway.Core.Models;

/// <summary>
/// 裂片机配置模型 - 用于配置文件绑定
/// </summary>
public class DicingMachineConfig
{
    /// <summary>
    /// 设备IP地址
    /// </summary>
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// 设备端口
    /// </summary>
    public int Port { get; set; } = 5000;

    /// <summary>
    /// 期望的裂片机编号
    /// </summary>
    public string? ExpectedMachineNumber { get; set; }

    /// <summary>
    /// 连接超时时间
    /// </summary>
    public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// 设备名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 设备描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 是否启用自动重连
    /// </summary>
    public bool EnableAutoReconnect { get; set; } = true;

    /// <summary>
    /// 心跳间隔 (秒)
    /// </summary>
    public int HeartbeatInterval { get; set; } = 30;

    /// <summary>
    /// 是否启用数据采集
    /// </summary>
    public bool EnableDataCollection { get; set; } = true;

    /// <summary>
    /// 数据采集间隔 (毫秒)
    /// </summary>
    public int DataCollectionInterval { get; set; } = 1000;

    /// <summary>
    /// 验证配置有效性
    /// </summary>
    public (bool IsValid, List<string> Errors) Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(IpAddress) || !System.Net.IPAddress.TryParse(IpAddress, out _))
        {
            errors.Add("无效的IP地址");
        }

        if (Port <= 0 || Port > 65535)
        {
            errors.Add("端口必须在1-65535范围内");
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            errors.Add("设备名称不能为空");
        }

        if (HeartbeatInterval <= 0)
        {
            errors.Add("心跳间隔必须大于0");
        }

        if (DataCollectionInterval <= 0)
        {
            errors.Add("数据采集间隔必须大于0");
        }

        return (errors.Count == 0, errors);
    }
}
