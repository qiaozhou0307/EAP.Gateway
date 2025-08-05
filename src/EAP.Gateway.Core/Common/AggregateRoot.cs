using EAP.Gateway.Core.Events.Common;

namespace EAP.Gateway.Core.Common;

/// <summary>
/// 聚合根基类，提供领域事件管理功能
/// </summary>
/// <typeparam name="TId">聚合根标识类型</typeparam>
public abstract class AggregateRoot<TId> : AggregateRoot
    where TId : notnull
{
    public TId Id { get; protected set; } = default!;

    protected AggregateRoot() { }

    protected AggregateRoot(TId id)
    {
        Id = id;
    }
}

/// <summary>
/// 聚合根基类，管理领域事件的发布和清理
/// </summary>
public abstract class AggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = new();

    /// <summary>
    /// 获取待发布的领域事件集合（只读）
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// 添加领域事件到待发布列表
    /// </summary>
    /// <param name="domainEvent">要添加的领域事件</param>
    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        if (domainEvent == null)
            throw new ArgumentNullException(nameof(domainEvent));

        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// 移除指定的领域事件
    /// </summary>
    /// <param name="domainEvent">要移除的领域事件</param>
    protected void RemoveDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Remove(domainEvent);
    }

    /// <summary>
    /// 清空所有待发布的领域事件
    /// 通常在事件发布完成后调用
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    /// <summary>
    /// 检查是否存在待发布的领域事件
    /// </summary>
    public bool HasDomainEvents => _domainEvents.Count > 0;
}
