using EAP.Gateway.Core.Common;

namespace EAP.Gateway.Core.Aggregates.EquipmentAggregate;

/// <summary>
/// 数据变量标识值对象
/// </summary>
public class DataVariableId : ValueObject
{
    public string Value { get; private set; }

    private DataVariableId() { } // EF Core

    private DataVariableId(string value)
    {
        Value = value;
    }

    public static DataVariableId Create() => new(Guid.NewGuid().ToString());
    public static DataVariableId Create(string value) => new(value);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
