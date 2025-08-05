using System.ComponentModel;
using System.Globalization;
using EAP.Gateway.Core.Common;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace EAP.Gateway.Core.Aggregates.EquipmentAggregate;

/// <summary>
/// 设备标识强类型值对象
/// </summary>
public sealed class EquipmentId : ValueObject
{
    /// <summary>
    /// 设备ID的字符串值
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// 内部构造函数，供 EF Core 和序列化使用
    /// </summary>
    internal EquipmentId(string value)
    {
        Value = value;
    }

    /// <summary>
    /// 创建设备ID的工厂方法
    /// </summary>
    /// <param name="value">设备ID字符串值</param>
    /// <returns>设备ID实例</returns>
    /// <exception cref="ArgumentException">当设备ID无效时抛出</exception>
    public static EquipmentId Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Equipment ID cannot be null or empty", nameof(value));

        if (value.Length > 50)
            throw new ArgumentException("Equipment ID cannot exceed 50 characters", nameof(value));

        // 验证设备ID格式：只能包含字母、数字、下划线和连字符
        if (!IsValidFormat(value))
            throw new ArgumentException("Equipment ID can only contain letters, numbers, underscores, and hyphens", nameof(value));

        return new EquipmentId(value);
    }

    /// <summary>
    /// 尝试创建设备ID，不抛出异常
    /// </summary>
    /// <param name="value">设备ID字符串值</param>
    /// <param name="equipmentId">创建的设备ID，失败时为null</param>
    /// <returns>是否创建成功</returns>
    public static bool TryCreate(string value, out EquipmentId? equipmentId)
    {
        try
        {
            equipmentId = Create(value);
            return true;
        }
        catch
        {
            equipmentId = null;
            return false;
        }
    }

    /// <summary>
    /// 生成新的设备ID（基于GUID）
    /// </summary>
    /// <param name="prefix">可选的前缀</param>
    /// <returns>新的设备ID</returns>
    public static EquipmentId NewId(string? prefix = null)
    {
        var guidPart = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        var value = string.IsNullOrEmpty(prefix)
            ? $"EQ_{guidPart}"
            : $"{prefix}_{guidPart}";

        return new EquipmentId(value);
    }

    /// <summary>
    /// 验证设备ID格式
    /// </summary>
    private static bool IsValidFormat(string value)
    {
        return value.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '-');
    }

    /// <summary>
    /// 获取用于相等性比较的组件
    /// </summary>
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    /// <summary>
    /// 转换为字符串
    /// </summary>
    public override string ToString() => Value;

    /// <summary>
    /// 隐式转换为字符串
    /// </summary>
    public static implicit operator string(EquipmentId equipmentId) => equipmentId.Value;

    /// <summary>
    /// 显式转换从字符串
    /// </summary>
    public static explicit operator EquipmentId(string value) => Create(value);

    /// <summary>
    /// 检查设备ID是否为空
    /// </summary>
    public bool IsEmpty => string.IsNullOrWhiteSpace(Value);

    /// <summary>
    /// 获取设备ID的显示名称（用于UI显示）
    /// </summary>
    public string DisplayName => Value;
}
