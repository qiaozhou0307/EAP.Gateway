using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using Secs4Net;

namespace EAP.Gateway.Infrastructure.Communications.SecsGem.Events;

/// <summary>
/// SECS消息接收事件参数（基础设施层事件）
/// 属于基础设施层，因为直接操作Secs4Net的SecsMessage
/// </summary>
public class SecsMessageReceivedEventArgs : EventArgs
{
    public EquipmentId EquipmentId { get; }
    public SecsMessage Message { get; }
    public DateTime ReceivedAt { get; }
    public long MessageLength { get; }

    public SecsMessageReceivedEventArgs(EquipmentId equipmentId, SecsMessage message, long messageLength = 0)
    {
        EquipmentId = equipmentId;
        Message = message;
        ReceivedAt = DateTime.UtcNow;
        MessageLength = messageLength;
    }

    public string MessageType => $"S{Message.S}F{Message.F}";
    public bool IsRequest => Message.F % 2 == 1;
    public bool IsResponse => Message.F % 2 == 0;
    public bool IsPrimaryMessage => IsRequest;
    public bool IsSecondaryMessage => IsResponse;

    /// <summary>
    /// 转换为领域事件
    /// </summary>
    public Core.Events.Message.SecsMessageReceivedEvent ToDomainEvent()
    {
        return new Core.Events.Message.SecsMessageReceivedEvent(
            EquipmentId, Message.S, Message.F, Message.ReplyExpected,
            0, ReceivedAt, (int)MessageLength);
    }
}

/// <summary>
/// SECS消息发送事件参数（基础设施层事件）
/// </summary>
public class SecsMessageSentEventArgs : EventArgs
{
    public EquipmentId EquipmentId { get; }
    public SecsMessage Message { get; }
    public DateTime SentAt { get; }
    public long MessageLength { get; }

    public SecsMessageSentEventArgs(EquipmentId equipmentId, SecsMessage message, long messageLength = 0)
    {
        EquipmentId = equipmentId;
        Message = message;
        SentAt = DateTime.UtcNow;
        MessageLength = messageLength;
    }

    public string MessageType => $"S{Message.S}F{Message.F}";
    public bool IsRequest => Message.F % 2 == 1;
    public bool IsResponse => Message.F % 2 == 0;

    /// <summary>
    /// 转换为领域事件
    /// </summary>
    public Core.Events.Message.SecsMessageSentEvent ToDomainEvent()
    {
        return new Core.Events.Message.SecsMessageSentEvent(
            EquipmentId, Message.S, Message.F, Message.ReplyExpected,
            0, SentAt, (int)MessageLength);
    }
}

/// <summary>
/// 消息超时事件参数（基础设施层事件）
/// </summary>
public class MessageTimeoutEventArgs : EventArgs
{
    public EquipmentId EquipmentId { get; }
    public SecsMessage Message { get; }
    public DateTime SentAt { get; }
    public DateTime TimeoutAt { get; }
    public TimeSpan Timeout { get; }

    public MessageTimeoutEventArgs(EquipmentId equipmentId, SecsMessage message, DateTime sentAt, TimeSpan timeout)
    {
        EquipmentId = equipmentId;
        Message = message;
        SentAt = sentAt;
        Timeout = timeout;
        TimeoutAt = DateTime.UtcNow;
    }

    public string MessageType => $"S{Message.S}F{Message.F}";
    public TimeSpan ElapsedTime => TimeoutAt - SentAt;

    /// <summary>
    /// 转换为领域事件
    /// </summary>
    public Core.Events.Message.MessageTimeoutEvent ToDomainEvent()
    {
        return new Core.Events.Message.MessageTimeoutEvent(
            EquipmentId, Message.S, Message.F, 0, SentAt, TimeoutAt);
    }
}

/// <summary>
/// SECS消息事件工厂
/// 用于创建和转换消息事件
/// </summary>
public static class SecsMessageEventFactory
{
    /// <summary>
    /// 创建消息接收事件
    /// </summary>
    public static SecsMessageReceivedEventArgs CreateReceived(EquipmentId equipmentId, SecsMessage message, long messageLength = 0)
    {
        return new SecsMessageReceivedEventArgs(equipmentId, message, messageLength);
    }

    /// <summary>
    /// 创建消息发送事件
    /// </summary>
    public static SecsMessageSentEventArgs CreateSent(EquipmentId equipmentId, SecsMessage message, long messageLength = 0)
    {
        return new SecsMessageSentEventArgs(equipmentId, message, messageLength);
    }

    /// <summary>
    /// 创建消息超时事件
    /// </summary>
    public static MessageTimeoutEventArgs CreateTimeout(EquipmentId equipmentId, SecsMessage message, DateTime sentAt, TimeSpan timeout)
    {
        return new MessageTimeoutEventArgs(equipmentId, message, sentAt, timeout);
    }
}
