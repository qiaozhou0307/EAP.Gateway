namespace EAP.Gateway.Core.Events.Common;

/// <summary>
/// 领域事件基类，提供事件的基础属性实现
/// </summary>
public abstract class DomainEventBase : IDomainEvent
{
    /// <summary>
    /// 事件发生时间（UTC时间）
    /// </summary>
    public DateTime OccurredOn { get; } = DateTime.UtcNow;

    /// <summary>
    /// 事件唯一标识
    /// </summary>
    public Guid EventId { get; } = Guid.NewGuid();

    /// <summary>
    /// 事件版本号，默认为1
    /// </summary>
    public virtual int Version => 1;

    /// <summary>
    /// 事件类型名称，默认使用类名
    /// </summary>
    public virtual string EventType => GetType().Name;

    /// <summary>
    /// 受保护的构造函数，确保事件的不可变性
    /// </summary>
    protected DomainEventBase()
    {
    }

    /// <summary>
    /// 重写ToString方法，提供事件的可读描述
    /// </summary>
    public override string ToString()
    {
        return $"{EventType} [Id: {EventId}, OccurredOn: {OccurredOn:yyyy-MM-dd HH:mm:ss.fff}]";
    }

    /// <summary>
    /// 重写Equals方法，基于EventId进行比较
    /// </summary>
    public override bool Equals(object? obj)
    {
        if (obj is not DomainEventBase other)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        if (GetType() != other.GetType())
            return false;

        return EventId.Equals(other.EventId);
    }

    /// <summary>
    /// 重写GetHashCode方法，基于EventId计算哈希码
    /// </summary>
    public override int GetHashCode()
    {
        return EventId.GetHashCode();
    }
}
