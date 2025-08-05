using MediatR;

namespace EAP.Gateway.Core.Events.Common;

/// <summary>
/// 领域事件标记接口
/// 继承MediatR的INotification以支持事件发布订阅
/// </summary>
public interface IDomainEvent : INotification
{
    /// <summary>
    /// 事件发生时间（UTC时间）
    /// </summary>
    DateTime OccurredOn { get; }

    /// <summary>
    /// 事件唯一标识
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// 事件版本号，用于事件溯源和兼容性
    /// </summary>
    int Version { get; }

    /// <summary>
    /// 事件类型名称
    /// </summary>
    string EventType { get; }
}
