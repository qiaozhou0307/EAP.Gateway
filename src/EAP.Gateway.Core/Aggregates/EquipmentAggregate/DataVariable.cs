using EAP.Gateway.Core.Common;

namespace EAP.Gateway.Core.ValueObjects;

/// <summary>
/// 数据变量值对象
/// </summary>
public class DataVariable : ValueObject
{
    /// <summary>
    /// 变量ID
    /// </summary>
    public uint Id { get; }

    /// <summary>
    /// 变量名称
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 变量值
    /// </summary>
    public object Value { get; }

    /// <summary>
    /// 数据类型
    /// </summary>
    public string DataType { get; }

    /// <summary>
    /// 单位
    /// </summary>
    public string? Unit { get; }

    /// <summary>
    /// 质量标识
    /// </summary>
    public string Quality { get; }

    /// <summary>
    /// 时间戳
    /// </summary>
    public DateTime Timestamp { get; }

    /// <summary>
    /// 构造函数
    /// </summary>
    public DataVariable(uint id, string name, object value, string dataType,
        string? unit = null, string quality = "Good", DateTime? timestamp = null)
    {
        Id = id;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Value = value ?? throw new ArgumentNullException(nameof(value));
        DataType = dataType ?? throw new ArgumentNullException(nameof(dataType));
        Unit = unit;
        Quality = quality ?? "Good";
        Timestamp = timestamp ?? DateTime.UtcNow;
    }

    /// <summary>
    /// 更新变量值
    /// </summary>
    /// <param name="newValue">新值</param>
    /// <param name="newQuality">新质量（可选）</param>
    /// <param name="newTimestamp">新时间戳（可选）</param>
    /// <returns>新的DataVariable实例</returns>
    public DataVariable UpdateValue(object newValue, string? newQuality = null, DateTime? newTimestamp = null)
    {
        return new DataVariable(
            Id,
            Name,
            newValue,
            newValue.GetType().Name,
            Unit,
            newQuality ?? Quality,
            newTimestamp ?? DateTime.UtcNow
        );
    }

    /// <summary>
    /// 检查数据是否有效
    /// </summary>
    public bool IsValid => Quality == "Good" || Quality == "Valid";

    /// <summary>
    /// 检查数据是否过期
    /// </summary>
    /// <param name="maxAge">最大年龄</param>
    /// <returns>是否过期</returns>
    public bool IsExpired(TimeSpan maxAge)
    {
        return DateTime.UtcNow - Timestamp > maxAge;
    }

    /// <summary>
    /// 获取类型化的值
    /// </summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <returns>转换后的值</returns>
    public T? GetValue<T>()
    {
        try
        {
            return (T)Convert.ChangeType(Value, typeof(T));
        }
        catch
        {
            return default(T);
        }
    }

    /// <summary>
    /// 尝试获取类型化的值
    /// </summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <param name="value">输出值</param>
    /// <returns>是否转换成功</returns>
    public bool TryGetValue<T>(out T? value)
    {
        try
        {
            value = (T)Convert.ChangeType(Value, typeof(T));
            return true;
        }
        catch
        {
            value = default(T);
            return false;
        }
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Id;
        yield return Name;
        yield return Value;
        yield return DataType;
        yield return Unit ?? string.Empty;
        yield return Quality;
        yield return Timestamp;
    }

    public override string ToString()
    {
        var unitStr = !string.IsNullOrEmpty(Unit) ? $" {Unit}" : string.Empty;
        return $"{Name}({Id}): {Value}{unitStr} [{Quality}] @ {Timestamp:yyyy-MM-dd HH:mm:ss}";
    }
}
