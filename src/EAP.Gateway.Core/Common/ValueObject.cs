namespace EAP.Gateway.Core.Common;

/// <summary>
/// 值对象基类，提供值相等性比较功能
/// 值对象是不可变的，通过值而非引用进行比较
/// </summary>
public abstract class ValueObject
{
    /// <summary>
    /// 相等操作符重载
    /// </summary>
    protected static bool EqualOperator(ValueObject? left, ValueObject? right)
    {
        if (ReferenceEquals(left, null) ^ ReferenceEquals(right, null))
            return false;

        return ReferenceEquals(left, null) || left.Equals(right);
    }

    /// <summary>
    /// 不等操作符重载
    /// </summary>
    protected static bool NotEqualOperator(ValueObject? left, ValueObject? right)
    {
        return !EqualOperator(left, right);
    }

    /// <summary>
    /// 获取用于相等性比较的组件
    /// 子类必须实现此方法来定义比较的属性
    /// </summary>
    /// <returns>用于相等性比较的属性值序列</returns>
    protected abstract IEnumerable<object?> GetEqualityComponents();

    /// <summary>
    /// 重写Equals方法，基于GetEqualityComponents进行比较
    /// </summary>
    public override bool Equals(object? obj)
    {
        if (obj == null || obj.GetType() != GetType())
            return false;

        var other = (ValueObject)obj;
        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    /// <summary>
    /// 重写GetHashCode方法，基于所有相等性组件计算哈希码
    /// </summary>
    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Select(x => x?.GetHashCode() ?? 0)
            .Aggregate((x, y) => x ^ y);
    }

    /// <summary>
    /// 相等操作符
    /// </summary>
    public static bool operator ==(ValueObject? one, ValueObject? two)
    {
        return EqualOperator(one, two);
    }

    /// <summary>
    /// 不等操作符
    /// </summary>
    public static bool operator !=(ValueObject? one, ValueObject? two)
    {
        return NotEqualOperator(one, two);
    }

    /// <summary>
    /// 创建值对象的副本（由于值对象不可变，实际返回自身）
    /// </summary>
    /// <returns>值对象副本</returns>
    public virtual ValueObject Copy()
    {
        return this;
    }
}
