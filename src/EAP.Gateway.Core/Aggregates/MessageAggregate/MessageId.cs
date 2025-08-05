using EAP.Gateway.Core.Common;

namespace EAP.Gateway.Core.Aggregates.MessageAggregate;

/// <summary>
/// 消息标识值对象
/// </summary>
public class MessageId : ValueObject
{
    public string Value { get; private set; }

    private MessageId() { } // EF Core

    private MessageId(string value)
    {
        Value = value;
    }

    public static MessageId Create() => new(Guid.NewGuid().ToString());
    public static MessageId Create(string value) => new(value);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
