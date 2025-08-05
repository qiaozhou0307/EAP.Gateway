using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using EAP.Gateway.Core.Events.Common;

namespace EAP.Gateway.Core.Events.Message;

/// <summary>
/// SECS消息接收事件
/// </summary>
public sealed class SecsMessageReceivedEvent : DomainEventBase
{
    public EquipmentId EquipmentId { get; }
    public byte Stream { get; }
    public byte Function { get; }
    public bool WaitBit { get; }
    public uint SystemBytes { get; }
    public DateTime ReceivedAt { get; }
    public int MessageLength { get; }

    public SecsMessageReceivedEvent(EquipmentId equipmentId, byte stream, byte function, bool waitBit, uint systemBytes, DateTime receivedAt, int messageLength = 0)
    {
        EquipmentId = equipmentId;
        Stream = stream;
        Function = function;
        WaitBit = waitBit;
        SystemBytes = systemBytes;
        ReceivedAt = receivedAt;
        MessageLength = messageLength;
    }

    public string MessageType => $"S{Stream}F{Function}";
    public bool IsRequest => Function % 2 == 1;
    public bool IsResponse => Function % 2 == 0;
}
