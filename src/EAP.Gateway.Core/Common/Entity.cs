namespace EAP.Gateway.Core.Common;

/// <summary>
/// 实体基类
/// </summary>
/// <typeparam name="TId">标识类型</typeparam>
public abstract class Entity<TId> : IEquatable<Entity<TId>>
    where TId : notnull
{
    public TId Id { get; protected set; }

    protected Entity(TId id)
    {
        Id = id;
    }

    public override bool Equals(object? obj)
    {
        return obj is Entity<TId> entity && Id.Equals(entity.Id);
    }

    public bool Equals(Entity<TId>? other)
    {
        return Equals((object?)other);
    }

    public static bool operator ==(Entity<TId> left, Entity<TId> right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(Entity<TId> left, Entity<TId> right)
    {
        return !Equals(left, right);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}

/// <summary>
/// 实体基类（Guid主键）
/// </summary>
public abstract class Entity : Entity<Guid>
{
    protected Entity() : base(Guid.NewGuid())
    {
    }

    protected Entity(Guid id) : base(id)
    {
    }
}
