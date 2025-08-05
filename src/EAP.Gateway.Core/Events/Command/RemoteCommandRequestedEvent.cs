using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using EAP.Gateway.Core.Entities;
using EAP.Gateway.Core.Events.Common;

namespace EAP.Gateway.Core.Events.Command;

/// <summary>
/// 远程命令请求事件
/// </summary>
public sealed class RemoteCommandRequestedEvent : DomainEventBase
{
    public EquipmentId EquipmentId { get; }
    public RemoteCommand Command { get; }
    public DateTime RequestedAt { get; }

    public RemoteCommandRequestedEvent(EquipmentId equipmentId, RemoteCommand command, DateTime requestedAt)
    {
        EquipmentId = equipmentId;
        Command = command;
        RequestedAt = requestedAt;
    }
}
