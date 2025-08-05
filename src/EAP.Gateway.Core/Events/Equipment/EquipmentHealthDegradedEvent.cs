using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using EAP.Gateway.Core.Events.Common;

namespace EAP.Gateway.Core.Events.Equipment;

/// <summary>
/// 设备健康降级事件
/// </summary>
public sealed class EquipmentHealthDegradedEvent : DomainEventBase
{
    public EquipmentId EquipmentId { get; }
    public HealthStatus NewHealthStatus { get; }
    public IReadOnlyList<string> Issues { get; }
    public DateTime DegradedAt { get; }

    public EquipmentHealthDegradedEvent(EquipmentId equipmentId, HealthStatus newHealthStatus, IEnumerable<string> issues, DateTime degradedAt)
    {
        EquipmentId = equipmentId;
        NewHealthStatus = newHealthStatus;
        Issues = issues.ToList().AsReadOnly();
        DegradedAt = degradedAt;
    }
}
