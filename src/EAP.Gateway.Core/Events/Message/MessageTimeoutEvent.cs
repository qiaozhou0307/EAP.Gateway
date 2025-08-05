using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using EAP.Gateway.Core.Events.Common;

namespace EAP.Gateway.Core.Events.Message;

/// <summary>
/// 消息超时事件
/// </summary>
public sealed class MessageTimeoutEvent : DomainEventBase
{
    public EquipmentId EquipmentId { get; }
    public byte Stream { get; }
    public byte Function { get; }
    public uint SystemBytes { get; }
    public DateTime SentAt { get; }
    public DateTime TimeoutAt { get; }
    public TimeSpan TimeoutDuration { get; }

    public MessageTimeoutEvent(EquipmentId equipmentId, byte stream, byte function, uint systemBytes, DateTime sentAt, DateTime timeoutAt)
    {
        EquipmentId = equipmentId;
        Stream = stream;
        Function = function;
        SystemBytes = systemBytes;
        SentAt = sentAt;
        TimeoutAt = timeoutAt;
        TimeoutDuration = timeoutAt - sentAt;
    }

    public string MessageType => $"S{Stream}F{Function}";
}
