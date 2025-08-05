using System.ComponentModel.DataAnnotations;

namespace EAP.Gateway.Api.Models.Requests;

/// <summary>
/// 发送命令请求模型
/// </summary>
public class SendCommandRequest
{
    /// <summary>
    /// 命令参数
    /// </summary>
    public Dictionary<string, object>? Parameters { get; set; }

    /// <summary>
    /// 是否等待执行结果
    /// </summary>
    public bool WaitForResult { get; set; } = true;

    /// <summary>
    /// 超时时间（秒）
    /// </summary>
    [Range(1, 3600)]
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// 请求备注
    /// </summary>
    [MaxLength(500)]
    public string? Notes { get; set; }
}
