using EAP.Gateway.Core.Events.Common;

namespace EAP.Gateway.Core.Events.System;

/// <summary>
/// 需要人工干预事件
/// </summary>
public sealed class InterventionRequiredEvent : DomainEventBase
{
    public string InterventionType { get; }
    public string Description { get; }
    public string? EquipmentId { get; }
    public InterventionPriority Priority { get; }
    public DateTime RequiredAt { get; }
    public IDictionary<string, object>? Context { get; }

    public InterventionRequiredEvent(string interventionType, string description, InterventionPriority priority, string? equipmentId = null, IDictionary<string, object>? context = null)
    {
        InterventionType = interventionType;
        Description = description;
        EquipmentId = equipmentId;
        Priority = priority;
        RequiredAt = DateTime.UtcNow;
        Context = context;
    }
}
/// <summary>
/// 干预优先级枚举
/// </summary>
public enum InterventionPriority
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}
