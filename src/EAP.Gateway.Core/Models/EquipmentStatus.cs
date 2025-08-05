using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using EAP.Gateway.Core.ValueObjects;

namespace EAP.Gateway.Core.Models;

/// <summary>
/// 设备状态领域模型（不是DTO）
/// 支持不可变更新模式
/// </summary>
public class EquipmentStatus
{
    public EquipmentId EquipmentId { get; private set; } = null!;
    public string Name { get; private set; } = string.Empty;
    public EquipmentState State { get; private set; }
    public string? SubState { get; private set; }
    public ConnectionState ConnectionState { get; private set; } = null!;
    public HealthStatus HealthStatus { get; private set; }
    public DateTime? LastHeartbeat { get; private set; }
    public DateTime? LastDataUpdate { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public ProcessingMetrics? Metrics { get; private set; }
    public int ActiveAlarmsCount { get; private set; }

    /// <summary>
    /// 私有构造函数 - 用于创建新实例
    /// </summary>
    private EquipmentStatus() { }

    /// <summary>
    /// 完整构造函数 - 用于不可变更新
    /// </summary>
    private EquipmentStatus(
        EquipmentId equipmentId,
        string name,
        EquipmentState state,
        string? subState,
        ConnectionState connectionState,
        HealthStatus healthStatus,
        DateTime? lastHeartbeat,
        DateTime? lastDataUpdate,
        DateTime updatedAt,
        ProcessingMetrics? metrics,
        int activeAlarmsCount)
    {
        EquipmentId = equipmentId;
        Name = name;
        State = state;
        SubState = subState;
        ConnectionState = connectionState;
        HealthStatus = healthStatus;
        LastHeartbeat = lastHeartbeat;
        LastDataUpdate = lastDataUpdate;
        UpdatedAt = updatedAt;
        Metrics = metrics;
        ActiveAlarmsCount = activeAlarmsCount;
    }

    /// <summary>
    /// 创建设备状态
    /// </summary>
    public static EquipmentStatus Create(
        EquipmentId equipmentId,
        string name,
        EquipmentState state,
        ConnectionState connectionState,
        HealthStatus healthStatus)
    {
        return new EquipmentStatus
        {
            EquipmentId = equipmentId,
            Name = name,
            State = state,
            ConnectionState = connectionState,
            HealthStatus = healthStatus,
            UpdatedAt = DateTime.UtcNow
        };
    }

    #region 不可变更新方法 (Immutable Update Methods)

    /// <summary>
    /// 更新设备状态（不可变方式）
    /// </summary>
    /// <param name="newState">新状态</param>
    /// <returns>新的设备状态实例</returns>
    public EquipmentStatus WithState(EquipmentState newState)
    {
        return new EquipmentStatus(
            EquipmentId, Name, newState, SubState, ConnectionState,
            HealthStatus, LastHeartbeat, LastDataUpdate, DateTime.UtcNow,
            Metrics, ActiveAlarmsCount);
    }

    /// <summary>
    /// 更新设备状态（字符串版本 - 用于兼容现有代码）
    /// </summary>
    /// <param name="newStateString">新状态字符串</param>
    /// <returns>新的设备状态实例</returns>
    public EquipmentStatus WithState(string newStateString)
    {
        if (Enum.TryParse<EquipmentState>(newStateString, true, out var newState))
        {
            return WithState(newState);
        }

        // 如果解析失败，保持原状态但更新时间
        return WithUpdatedAt(DateTime.UtcNow);
    }

    /// <summary>
    /// 更新子状态（不可变方式）
    /// </summary>
    /// <param name="newSubState">新子状态</param>
    /// <returns>新的设备状态实例</returns>
    public EquipmentStatus WithSubState(string? newSubState)
    {
        return new EquipmentStatus(
            EquipmentId, Name, State, newSubState, ConnectionState,
            HealthStatus, LastHeartbeat, LastDataUpdate, DateTime.UtcNow,
            Metrics, ActiveAlarmsCount);
    }

    /// <summary>
    /// 更新连接状态（不可变方式）
    /// </summary>
    /// <param name="newConnectionState">新的连接状态</param>
    /// <returns>新的设备状态实例</returns>
    public EquipmentStatus WithConnectionState(ConnectionState newConnectionState)
    {
        return new EquipmentStatus(
            EquipmentId, Name, State, SubState, newConnectionState,
            HealthStatus, newConnectionState.IsConnected ? DateTime.UtcNow : LastHeartbeat,
            LastDataUpdate, DateTime.UtcNow, Metrics, ActiveAlarmsCount);
    }

    /// <summary>
    /// 更新健康状态（不可变方式）
    /// </summary>
    /// <param name="newHealthStatus">新的健康状态</param>
    /// <returns>新的设备状态实例</returns>
    public EquipmentStatus WithHealthStatus(HealthStatus newHealthStatus)
    {
        return new EquipmentStatus(
            EquipmentId, Name, State, SubState, ConnectionState,
            newHealthStatus, LastHeartbeat, LastDataUpdate, DateTime.UtcNow,
            Metrics, ActiveAlarmsCount);
    }

    /// <summary>
    /// 更新最后数据更新时间（不可变方式）
    /// </summary>
    /// <param name="lastDataUpdate">最后数据更新时间</param>
    /// <returns>新的设备状态实例</returns>
    public EquipmentStatus WithLastDataUpdate(DateTime lastDataUpdate)
    {
        return new EquipmentStatus(
            EquipmentId, Name, State, SubState, ConnectionState,
            HealthStatus, LastHeartbeat, lastDataUpdate, DateTime.UtcNow,
            Metrics, ActiveAlarmsCount);
    }

    /// <summary>
    /// 更新最后心跳时间（不可变方式）
    /// </summary>
    /// <param name="lastHeartbeat">最后心跳时间</param>
    /// <returns>新的设备状态实例</returns>
    public EquipmentStatus WithLastHeartbeat(DateTime? lastHeartbeat)
    {
        return new EquipmentStatus(
            EquipmentId, Name, State, SubState, ConnectionState,
            HealthStatus, lastHeartbeat, LastDataUpdate, DateTime.UtcNow,
            Metrics, ActiveAlarmsCount);
    }

    /// <summary>
    /// 更新更新时间（不可变方式）
    /// </summary>
    /// <param name="updatedAt">更新时间</param>
    /// <returns>新的设备状态实例</returns>
    public EquipmentStatus WithUpdatedAt(DateTime updatedAt)
    {
        return new EquipmentStatus(
            EquipmentId, Name, State, SubState, ConnectionState,
            HealthStatus, LastHeartbeat, LastDataUpdate, updatedAt,
            Metrics, ActiveAlarmsCount);
    }

    /// <summary>
    /// 更新处理指标（不可变方式）
    /// </summary>
    /// <param name="newMetrics">新的处理指标</param>
    /// <returns>新的设备状态实例</returns>
    public EquipmentStatus WithMetrics(ProcessingMetrics? newMetrics)
    {
        return new EquipmentStatus(
            EquipmentId, Name, State, SubState, ConnectionState,
            HealthStatus, LastHeartbeat, LastDataUpdate, DateTime.UtcNow,
            newMetrics, ActiveAlarmsCount);
    }

    /// <summary>
    /// 更新活动报警数量（不可变方式）
    /// </summary>
    /// <param name="newActiveAlarmsCount">新的活动报警数量</param>
    /// <returns>新的设备状态实例</returns>
    public EquipmentStatus WithActiveAlarmsCount(int newActiveAlarmsCount)
    {
        return new EquipmentStatus(
            EquipmentId, Name, State, SubState, ConnectionState,
            HealthStatus, LastHeartbeat, LastDataUpdate, DateTime.UtcNow,
            Metrics, Math.Max(0, newActiveAlarmsCount));
    }

    /// <summary>
    /// 复合更新 - 状态和子状态（不可变方式）
    /// </summary>
    /// <param name="newState">新状态</param>
    /// <param name="newSubState">新子状态</param>
    /// <returns>新的设备状态实例</returns>
    public EquipmentStatus WithStateAndSubState(EquipmentState newState, string? newSubState = null)
    {
        return new EquipmentStatus(
            EquipmentId, Name, newState, newSubState, ConnectionState,
            HealthStatus, LastHeartbeat, LastDataUpdate, DateTime.UtcNow,
            Metrics, ActiveAlarmsCount);
    }

    #endregion

    #region 可变更新方法 (Mutable Update Methods) - 保留用于兼容性

    /// <summary>
    /// 更新状态（可变方式） - 已废弃，推荐使用 WithState
    /// </summary>
    [Obsolete("使用 WithState 方法进行不可变更新", false)]
    public void UpdateState(EquipmentState newState, string? subState = null)
    {
        State = newState;
        SubState = subState;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 更新连接状态（可变方式） - 已废弃，推荐使用 WithConnectionState
    /// </summary>
    [Obsolete("使用 WithConnectionState 方法进行不可变更新", false)]
    public void UpdateConnectionState(ConnectionState newConnectionState)
    {
        ConnectionState = newConnectionState;
        UpdatedAt = DateTime.UtcNow;
        LastHeartbeat = newConnectionState.IsConnected ? DateTime.UtcNow : LastHeartbeat;
    }

    /// <summary>
    /// 更新健康状态（可变方式） - 已废弃，推荐使用 WithHealthStatus
    /// </summary>
    [Obsolete("使用 WithHealthStatus 方法进行不可变更新", false)]
    public void UpdateHealthStatus(HealthStatus newHealthStatus)
    {
        HealthStatus = newHealthStatus;
        UpdatedAt = DateTime.UtcNow;
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 判断是否在线
    /// </summary>
    public bool IsOnline => ConnectionState.IsConnected && State.IsAvailable();

    /// <summary>
    /// 判断是否需要关注
    /// </summary>
    public bool RequiresAttention => State.RequiresAttention() || ActiveAlarmsCount > 0;

    /// <summary>
    /// 获取状态摘要
    /// </summary>
    public string GetStatusSummary()
    {
        var statusParts = new List<string> { State.GetDisplayName() };

        if (!string.IsNullOrEmpty(SubState))
            statusParts.Add($"({SubState})");

        if (!ConnectionState.IsConnected)
            statusParts.Add("[离线]");

        if (ActiveAlarmsCount > 0)
            statusParts.Add($"[{ActiveAlarmsCount}个报警]");

        return string.Join(" ", statusParts);
    }

    #endregion

    public override string ToString()
    {
        return $"设备: {Name} ({EquipmentId}), 状态: {GetStatusSummary()}, 更新时间: {UpdatedAt:yyyy-MM-dd HH:mm:ss}";
    }
}
