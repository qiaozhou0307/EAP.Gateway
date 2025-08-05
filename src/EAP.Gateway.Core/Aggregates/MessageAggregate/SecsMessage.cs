using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using EAP.Gateway.Core.Common;

namespace EAP.Gateway.Core.Aggregates.MessageAggregate;

/// <summary>
/// SECS消息聚合根 - 使用现有Entity基类
/// </summary>
public class SecsMessage : Entity<MessageId>
{
    public EquipmentId EquipmentId { get; private set; }
    public byte Stream { get; private set; }
    public byte Function { get; private set; }
    public uint SystemBytes { get; private set; }
    public MessageDirection Direction { get; private set; }
    public DateTime Timestamp { get; private set; }
    public string? Data { get; private set; }

    private SecsMessage() : base(MessageId.Create()) { } // EF Core

    public SecsMessage(MessageId id, EquipmentId equipmentId, byte stream, byte function, uint systemBytes, MessageDirection direction)
        : base(id)
    {
        EquipmentId = equipmentId;
        Stream = stream;
        Function = function;
        SystemBytes = systemBytes;
        Direction = direction;
        Timestamp = DateTime.UtcNow;
    }

    public void SetData(string data)
    {
        Data = data;
    }
}
