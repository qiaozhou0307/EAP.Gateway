using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using EAP.Gateway.Core.Common;
using EAP.Gateway.Core.ValueObjects;

namespace EAP.Gateway.Core.Entities;

/// <summary>
/// 远程命令实体
/// </summary>
public record RemoteCommand
{
    /// <summary>
    /// 命令ID
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// 设备ID
    /// </summary>
    [Required, MaxLength(50)]
    public string EquipmentId { get; init; } = string.Empty;

    /// <summary>
    /// 命令名称
    /// </summary>
    [Required, MaxLength(100)]
    public string CommandName { get; init; } = string.Empty;

    /// <summary>
    /// 命令参数（JSON格式）
    /// </summary>
    [MaxLength(2000)]
    public string? Parameters { get; init; }

    /// <summary>
    /// 命令状态
    /// </summary>
    public CommandStatus Status { get; init; }

    /// <summary>
    /// 请求者
    /// </summary>
    [MaxLength(100)]
    public string? RequestedBy { get; init; }

    /// <summary>
    /// 请求时间
    /// </summary>
    public DateTime RequestedAt { get; init; }

    /// <summary>
    /// 完成时间
    /// </summary>
    public DateTime? CompletedAt { get; init; }

    /// <summary>
    /// 超时时间
    /// </summary>
    public DateTime TimeoutAt { get; init; }

    /// <summary>
    /// 结果消息
    /// </summary>
    [MaxLength(1000)]
    public string? ResultMessage { get; init; }

    /// <summary>
    /// 结果数据（JSON格式）
    /// </summary>
    [MaxLength(4000)]
    public string? ResultDataJson { get; init; }

    /// <summary>
    /// 结果数据（运行时属性）
    /// </summary>
    public IReadOnlyDictionary<string, object>? ResultData { get; init; }

    /// <summary>
    /// 重试次数
    /// </summary>
    public int RetryCount { get; init; }

    /// <summary>
    /// 最大重试次数
    /// </summary>
    public int MaxRetries { get; init; } = 3;

    /// <summary>
    /// 优先级
    /// </summary>
    public CommandPriority Priority { get; init; } = CommandPriority.Normal;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// 检查命令是否超时
    /// </summary>
    public bool IsTimeout => DateTime.UtcNow > TimeoutAt;

    /// <summary>
    /// 检查命令是否可以重试
    /// </summary>
    public bool CanRetry => RetryCount < MaxRetries &&
                           (Status == CommandStatus.Failed || Status == CommandStatus.Timeout);

    /// <summary>
    /// 检查命令是否已完成
    /// </summary>
    public bool IsCompleted => Status == CommandStatus.Completed ||
                              Status == CommandStatus.Failed ||
                              Status == CommandStatus.Cancelled ||
                              Status == CommandStatus.Timeout;

    /// <summary>
    /// 获取执行时间
    /// </summary>
    public TimeSpan? ExecutionTime => CompletedAt.HasValue ? CompletedAt.Value - RequestedAt : null;

    /// <summary>
    /// 获取反序列化的参数字典
    /// </summary>
    public IDictionary<string, object>? GetParametersDictionary()
    {
        if (string.IsNullOrEmpty(Parameters))
            return null;

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object>>(Parameters);
        }
        catch
        {
            return null;
        }
    }
}
