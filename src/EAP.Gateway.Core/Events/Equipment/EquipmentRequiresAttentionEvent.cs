using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using EAP.Gateway.Core.Events.Common;
using EAP.Gateway.Core.ValueObjects;

namespace EAP.Gateway.Core.Events.Equipment;

/// <summary>
/// 设备需要关注事件
/// </summary>
public sealed class EquipmentRequiresAttentionEvent : DomainEventBase
{
    public EquipmentId EquipmentId { get; }
    public EquipmentState CurrentState { get; }
    public string? Reason { get; }
    public DateTime RequiresAttentionAt { get; }
    public AttentionLevel Level { get; }

    public EquipmentRequiresAttentionEvent(EquipmentId equipmentId, EquipmentState currentState, string? reason, DateTime requiresAttentionAt)
    {
        EquipmentId = equipmentId;
        CurrentState = currentState;
        Reason = reason;
        RequiresAttentionAt = requiresAttentionAt;
        Level = DetermineAttentionLevel(currentState);
    }

    private static AttentionLevel DetermineAttentionLevel(EquipmentState state)
    {
        return state switch
        {
            EquipmentState.FAULT => AttentionLevel.Critical,
            EquipmentState.DOWN => AttentionLevel.High,
            EquipmentState.ALARM => AttentionLevel.Medium,
            EquipmentState.MAINTENANCE => AttentionLevel.Low,
            _ => AttentionLevel.Low
        };
    }
}

/// <summary>
/// 关注级别枚举
/// </summary>
public enum AttentionLevel
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}
