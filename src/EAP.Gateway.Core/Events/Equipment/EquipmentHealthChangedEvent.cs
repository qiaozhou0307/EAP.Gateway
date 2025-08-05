using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using EAP.Gateway.Core.Events.Common;
using EAP.Gateway.Core.ValueObjects;

namespace EAP.Gateway.Core.Events.Equipment;

/// <summary>
/// 设备健康状态变化事件
/// </summary>
public sealed class EquipmentHealthChangedEvent : DomainEventBase
{
    public EquipmentId EquipmentId { get; }
    public HealthStatus PreviousHealthStatus { get; }
    public HealthStatus NewHealthStatus { get; }
    public string? Reason { get; }
    public DateTime ChangedAt { get; }
    public IReadOnlyList<string>? HealthIssues { get; }

    public EquipmentHealthChangedEvent(
        EquipmentId equipmentId,
        HealthStatus previousHealthStatus,
        HealthStatus newHealthStatus,
        DateTime changedAt,
        string? reason = null,
        IEnumerable<string>? healthIssues = null)
    {
        EquipmentId = equipmentId ?? throw new ArgumentNullException(nameof(equipmentId));
        PreviousHealthStatus = previousHealthStatus;
        NewHealthStatus = newHealthStatus;
        ChangedAt = changedAt;
        Reason = reason;
        HealthIssues = healthIssues?.ToList().AsReadOnly();
    }
}
