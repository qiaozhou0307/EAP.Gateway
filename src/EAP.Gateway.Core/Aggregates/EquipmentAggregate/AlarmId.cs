using EAP.Gateway.Core.Common;

namespace EAP.Gateway.Core.Aggregates.EquipmentAggregate;

/// <summary>
/// 报警标识值对象 - 使用现有ValueObject基类
/// </summary>
public class AlarmId : ValueObject
{
    public string Value { get; private set; }

    private AlarmId() { Value = string.Empty; } // EF Core

    private AlarmId(string value)
    {
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }

    public static AlarmId Create() => new(Guid.NewGuid().ToString());
    public static AlarmId Create(string value) => new(value);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
