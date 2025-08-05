using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using EAP.Gateway.Core.Events.Common;

namespace EAP.Gateway.Core.Events.Equipment;

/// <summary>
/// 设备健康恢复事件
/// </summary>
public sealed class EquipmentHealthRecoveredEvent : DomainEventBase
{
    public EquipmentId EquipmentId { get; }
    public HealthStatus NewHealthStatus { get; }
    public DateTime RecoveredAt { get; }

    public EquipmentHealthRecoveredEvent(EquipmentId equipmentId, HealthStatus newHealthStatus, DateTime recoveredAt)
    {
        EquipmentId = equipmentId;
        NewHealthStatus = newHealthStatus;
        RecoveredAt = recoveredAt;
    }
}
