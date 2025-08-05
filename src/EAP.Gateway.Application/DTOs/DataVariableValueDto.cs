namespace EAP.Gateway.Application.DTOs;

/// <summary>
/// 数据变量值DTO
/// </summary>
public class DataVariableValueDto
{
    /// <summary>
    /// 变量ID
    /// </summary>
    public required uint Id { get; set; }

    /// <summary>
    /// 变量名称
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// 变量值
    /// </summary>
    public required object Value { get; set; }

    /// <summary>
    /// 数据类型
    /// </summary>
    public required string DataType { get; set; }

    /// <summary>
    /// 单位
    /// </summary>
    public string? Unit { get; set; }

    /// <summary>
    /// 时间戳
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 质量标识
    /// </summary>
    public string? Quality { get; set; } = "Good";

    /// <summary>
    /// 是否有效
    /// </summary>
    public bool IsValid => Quality == "Good" || Quality == "Valid";

    /// <summary>
    /// 获取类型化的值
    /// </summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <returns>转换后的值</returns>
    public T? GetValue<T>()
    {
        try
        {
            if (Value is T directValue)
                return directValue;

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
            if (Value is T directValue)
            {
                value = directValue;
                return true;
            }

            value = (T)Convert.ChangeType(Value, typeof(T));
            return true;
        }
        catch
        {
            value = default(T);
            return false;
        }
    }

    /// <summary>
    /// 检查数据是否过期
    /// </summary>
    /// <param name="maxAge">最大年龄</param>
    /// <returns>是否过期</returns>
    public bool IsExpired(TimeSpan maxAge)
    {
        return DateTime.UtcNow - Timestamp > maxAge;
    }

    public override string ToString()
    {
        var unitStr = !string.IsNullOrEmpty(Unit) ? $" {Unit}" : string.Empty;
        return $"{Name}({Id}): {Value}{unitStr} [{Quality}] @ {Timestamp:HH:mm:ss}";
    }
}
