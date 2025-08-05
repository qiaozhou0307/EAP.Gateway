using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using EAP.Gateway.Core.Events.Common;
using EAP.Gateway.Core.ValueObjects;

namespace EAP.Gateway.Core.Events.Equipment;

/// <summary>
/// 设备状态变化事件
/// 当设备运行状态发生变化时发布
/// </summary>
public sealed class EquipmentStatusChangedEvent : DomainEventBase
{
    /// <summary>
    /// 设备标识
    /// </summary>
    public EquipmentId EquipmentId { get; }

    /// <summary>
    /// 之前的设备状态
    /// </summary>
    public EquipmentState PreviousState { get; }

    /// <summary>
    /// 新的设备状态
    /// </summary>
    public EquipmentState NewState { get; }

    /// <summary>
    /// 状态变化原因
    /// </summary>
    public string? Reason { get; }

    /// <summary>
    /// 状态变化时间
    /// </summary>
    public DateTime ChangedAt { get; }

    /// <summary>
    /// 触发状态变化的操作员或系统
    /// </summary>
    public string? ChangedBy { get; }

    /// <summary>
    /// 状态变化类型
    /// </summary>
    public StateChangeType ChangeType { get; }

    /// <summary>
    /// 子状态信息
    /// </summary>
    public string? SubState { get; }

    /// <summary>
    /// 状态变化的上下文信息
    /// </summary>
    public IDictionary<string, object>? Context { get; }

    /// <summary>
    /// 是否为自动状态变化
    /// </summary>
    public bool IsAutomaticChange { get; }

    /// <summary>
    /// 状态持续时间（从上一个状态开始）
    /// </summary>
    public TimeSpan? PreviousStateDuration { get; }

    /// <summary>
    /// 构造函数
    /// </summary>
    public EquipmentStatusChangedEvent(
        EquipmentId equipmentId,
        EquipmentState previousState,
        EquipmentState newState,
        string? reason = null,
        DateTime? changedAt = null,
        string? changedBy = null,
        StateChangeType changeType = StateChangeType.Normal,
        string? subState = null,
        IDictionary<string, object>? context = null,
        bool isAutomaticChange = false,
        TimeSpan? previousStateDuration = null)
    {
        EquipmentId = equipmentId ?? throw new ArgumentNullException(nameof(equipmentId));
        PreviousState = previousState;
        NewState = newState;
        Reason = reason;
        ChangedAt = changedAt ?? DateTime.UtcNow;
        ChangedBy = changedBy;
        ChangeType = changeType;
        SubState = subState;
        Context = context != null ? new Dictionary<string, object>(context) : null;
        IsAutomaticChange = isAutomaticChange;
        PreviousStateDuration = previousStateDuration;
    }

    /// <summary>
    /// 创建普通状态变化事件
    /// </summary>
    /// <param name="equipmentId">设备标识</param>
    /// <param name="previousState">之前状态</param>
    /// <param name="newState">新状态</param>
    /// <param name="reason">变化原因</param>
    /// <returns>状态变化事件</returns>
    public static EquipmentStatusChangedEvent Create(
        EquipmentId equipmentId,
        EquipmentState previousState,
        EquipmentState newState,
        string? reason = null)
    {
        return new EquipmentStatusChangedEvent(equipmentId, previousState, newState, reason);
    }

    /// <summary>
    /// 创建操作员触发的状态变化事件
    /// </summary>
    /// <param name="equipmentId">设备标识</param>
    /// <param name="previousState">之前状态</param>
    /// <param name="newState">新状态</param>
    /// <param name="operatorId">操作员ID</param>
    /// <param name="reason">变化原因</param>
    /// <returns>状态变化事件</returns>
    public static EquipmentStatusChangedEvent CreateOperatorTriggered(
        EquipmentId equipmentId,
        EquipmentState previousState,
        EquipmentState newState,
        string operatorId,
        string? reason = null)
    {
        return new EquipmentStatusChangedEvent(
            equipmentId, previousState, newState, reason,
            changedBy: operatorId, changeType: StateChangeType.OperatorTriggered);
    }

    /// <summary>
    /// 创建自动状态变化事件
    /// </summary>
    /// <param name="equipmentId">设备标识</param>
    /// <param name="previousState">之前状态</param>
    /// <param name="newState">新状态</param>
    /// <param name="reason">变化原因</param>
    /// <param name="context">上下文信息</param>
    /// <returns>状态变化事件</returns>
    public static EquipmentStatusChangedEvent CreateAutomatic(
        EquipmentId equipmentId,
        EquipmentState previousState,
        EquipmentState newState,
        string reason,
        IDictionary<string, object>? context = null)
    {
        return new EquipmentStatusChangedEvent(
            equipmentId, previousState, newState, reason,
            changeType: StateChangeType.Automatic, context: context, isAutomaticChange: true);
    }

    /// <summary>
    /// 创建异常状态变化事件（如故障）
    /// </summary>
    /// <param name="equipmentId">设备标识</param>
    /// <param name="previousState">之前状态</param>
    /// <param name="errorReason">错误原因</param>
    /// <param name="context">错误上下文</param>
    /// <returns>状态变化事件</returns>
    public static EquipmentStatusChangedEvent CreateError(
        EquipmentId equipmentId,
        EquipmentState previousState,
        string errorReason,
        IDictionary<string, object>? context = null)
    {
        return new EquipmentStatusChangedEvent(
            equipmentId, previousState, EquipmentState.FAULT, errorReason,
            changeType: StateChangeType.Error, context: context, isAutomaticChange: true);
    }

    /// <summary>
    /// 检查是否为严重状态变化
    /// </summary>
    public bool IsCriticalChange =>
        NewState.RequiresAttention() ||
        ChangeType == StateChangeType.Error ||
        (PreviousState.IsAvailable() && !NewState.IsAvailable());

    /// <summary>
    /// 检查是否为恢复性状态变化
    /// </summary>
    public bool IsRecoveryChange =>
        !PreviousState.IsAvailable() && NewState.IsAvailable();

    /// <summary>
    /// 获取状态变化的严重程度
    /// </summary>
    public int GetSeverityLevel()
    {
        if (ChangeType == StateChangeType.Error)
            return 5;

        if (IsCriticalChange)
            return Math.Max(PreviousState.GetSeverityLevel(), NewState.GetSeverityLevel());

        if (IsRecoveryChange)
            return 1; // 恢复事件优先级较低

        return 2; // 正常状态变化
    }

    public override string ToString()
    {
        var reasonInfo = !string.IsNullOrEmpty(Reason) ? $" - {Reason}" : "";
        var changedByInfo = !string.IsNullOrEmpty(ChangedBy) ? $" by {ChangedBy}" : "";
        return $"Equipment {EquipmentId} state changed: {PreviousState} → {NewState}{changedByInfo}{reasonInfo}";
    }
}

/// <summary>
/// 状态变化类型枚举
/// </summary>
public enum StateChangeType
{
    /// <summary>
    /// 正常状态变化
    /// </summary>
    Normal = 0,

    /// <summary>
    /// 操作员触发的状态变化
    /// </summary>
    OperatorTriggered = 1,

    /// <summary>
    /// 自动状态变化
    /// </summary>
    Automatic = 2,

    /// <summary>
    /// 系统触发的状态变化
    /// </summary>
    SystemTriggered = 3,

    /// <summary>
    /// 错误导致的状态变化
    /// </summary>
    Error = 4,

    /// <summary>
    /// 报警导致的状态变化
    /// </summary>
    AlarmTriggered = 5,

    /// <summary>
    /// 维护相关的状态变化
    /// </summary>
    Maintenance = 6
}
