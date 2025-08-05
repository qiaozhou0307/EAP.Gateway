using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using EAP.Gateway.Core.Events.Common;
using EAP.Gateway.Core.ValueObjects;

namespace EAP.Gateway.Core.Events.Command;

/// <summary>
/// 远程命令状态更新事件
/// </summary>
public sealed class RemoteCommandStatusUpdatedEvent : DomainEventBase
{
    public EquipmentId EquipmentId { get; }
    public Guid CommandId { get; }
    public CommandStatus NewStatus { get; }
    public CommandStatus? PreviousStatus { get; }
    public string? ResultMessage { get; }
    public DateTime UpdatedAt { get; }

    public RemoteCommandStatusUpdatedEvent(EquipmentId equipmentId, Guid commandId, CommandStatus newStatus, string? resultMessage, DateTime updatedAt, CommandStatus? previousStatus = null)
    {
        EquipmentId = equipmentId;
        CommandId = commandId;
        NewStatus = newStatus;
        PreviousStatus = previousStatus;
        ResultMessage = resultMessage;
        UpdatedAt = updatedAt;
    }
}
