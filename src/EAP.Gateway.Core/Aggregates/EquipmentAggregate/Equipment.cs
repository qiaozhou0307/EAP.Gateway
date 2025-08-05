using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using EAP.Gateway.Core.Common;
using EAP.Gateway.Core.Entities;
using EAP.Gateway.Core.Events.Alarm;
using EAP.Gateway.Core.Events.Command;
using EAP.Gateway.Core.Events.Data;
using EAP.Gateway.Core.Events.Equipment;
using EAP.Gateway.Core.ValueObjects;

namespace EAP.Gateway.Core.Aggregates.EquipmentAggregate;

/// <summary>
/// 设备聚合根，管理设备的完整生命周期
/// 包括连接状态、运行状态、报警、数据采集等核心业务逻辑
/// 新增设备基础信息属性
/// </summary>
public class Equipment : AggregateRoot<EquipmentId>
{
    private readonly List<AlarmEvent> _activeAlarms = new();
    private readonly List<TraceData> _recentTraceData = new();
    private readonly List<RemoteCommand> _commandHistory = new();

    /// <summary>
    /// 设备名称
    /// </summary>
    [Required, MaxLength(255)]
    public string Name { get; private set; }

    /// <summary>
    /// 设备描述
    /// </summary>
    [MaxLength(1000)]
    public string Description { get; private set; }

    /// <summary>
    /// 设备制造商 - 新增属性
    /// </summary>
    [MaxLength(100)]
    public string? Manufacturer { get; private set; }

    /// <summary>
    /// 设备型号 - 新增属性
    /// </summary>
    [MaxLength(100)]
    public string? Model { get; private set; }

    /// <summary>
    /// 设备序列号 - 新增属性
    /// </summary>
    [MaxLength(100)]
    public string? SerialNumber { get; private set; }

    /// <summary>
    /// 数据采集间隔（秒） - 新增属性
    /// </summary>
    public int? DataCollectionInterval { get; private set; }

    /// <summary>
    /// 是否启用报警采集 - 新增属性
    /// </summary>
    public bool EnableAlarmCollection { get; private set; }

    /// <summary>
    /// 设备配置
    /// </summary>
    public EquipmentConfiguration Configuration { get; private set; }

    /// <summary>
    /// 连接状态
    /// </summary>
    public ConnectionState ConnectionState { get; private set; }

    /// <summary>
    /// 设备运行状态
    /// </summary>
    public EquipmentState State { get; private set; }

    /// <summary>
    /// 子状态描述
    /// </summary>
    [MaxLength(100)]
    public string? SubState { get; private set; }

    /// <summary>
    /// 最后心跳时间
    /// </summary>
    public DateTime? LastHeartbeat { get; private set; }

    /// <summary>
    /// 最后数据更新时间
    /// </summary>
    public DateTime? LastDataUpdate { get; private set; }

    /// <summary>
    /// 处理指标统计
    /// </summary>
    public ProcessingMetrics Metrics { get; private set; }

    /// <summary>
    /// 健康状态
    /// </summary>
    public HealthStatus HealthStatus { get; private set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    /// 创建者
    /// </summary>
    [MaxLength(100)]
    public string? CreatedBy { get; private set; }

    /// <summary>
    /// 最后更新者
    /// </summary>
    [MaxLength(100)]
    public string? UpdatedBy { get; private set; }

    /// <summary>
    /// 活动报警列表（只读）
    /// </summary>
    public IReadOnlyList<AlarmEvent> ActiveAlarms => _activeAlarms.AsReadOnly();

    /// <summary>
    /// 最近的追踪数据（只读）
    /// </summary>
    public IReadOnlyList<TraceData> RecentTraceData => _recentTraceData.AsReadOnly();

    /// <summary>
    /// 远程命令历史（只读）
    /// </summary>
    public IReadOnlyList<RemoteCommand> CommandHistory => _commandHistory.AsReadOnly();

    /// <summary>
    /// EF Core构造函数
    /// </summary>
    private Equipment()
    {
        // 初始化必需的属性以避免编译器警告
        Name = string.Empty;
        Description = string.Empty;
        Configuration = null!;
        ConnectionState = null!;
        Metrics = null!;
    }

    /// <summary>
    /// 聚合根工厂方法 - 改进版本
    /// </summary>
    /// <param name="id">设备标识</param>
    /// <param name="name">设备名称</param>
    /// <param name="description">设备描述</param>
    /// <param name="configuration">设备配置</param>
    /// <param name="manufacturer">制造商</param>
    /// <param name="model">设备型号</param>
    /// <param name="serialNumber">序列号</param>
    /// <param name="dataCollectionInterval">数据采集间隔</param>
    /// <param name="enableAlarmCollection">是否启用报警采集</param>
    /// <param name="createdBy">创建者</param>
    /// <returns>设备聚合根实例</returns>
    public static Equipment Create(
        EquipmentId id,
        string name,
        string description,
        EquipmentConfiguration configuration,
        string? manufacturer = null,
        string? model = null,
        string? serialNumber = null,
        int? dataCollectionInterval = null,
        bool enableAlarmCollection = true,
        string? createdBy = null)
    {
        // 参数验证
        if (id == null)
            throw new ArgumentNullException(nameof(id));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Equipment name cannot be null or empty", nameof(name));
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));
        if (!configuration.IsValid)
            throw new ArgumentException("Equipment configuration is invalid", nameof(configuration));

        var now = DateTime.UtcNow;
        var equipment = new Equipment
        {
            Id = id,
            Name = name.Trim(),
            Description = description?.Trim() ?? string.Empty,
            Manufacturer = manufacturer?.Trim(),
            Model = model?.Trim(),
            SerialNumber = serialNumber?.Trim(),
            DataCollectionInterval = dataCollectionInterval,
            EnableAlarmCollection = enableAlarmCollection,
            Configuration = configuration,
            ConnectionState = ConnectionState.Initial(),
            State = EquipmentState.UNKNOWN,
            SubState = null,
            LastHeartbeat = null,
            LastDataUpdate = null,
            Metrics = ProcessingMetrics.Empty(),
            HealthStatus = HealthStatus.Unknown,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = createdBy,
            UpdatedBy = createdBy
        };

        // 发布设备创建事件
        equipment.AddDomainEvent(new EquipmentCreatedEvent(id, name, configuration.Endpoint, now, createdBy));

        return equipment;
    }

    /// <summary>
    /// 更新设备基础信息
    /// </summary>
    /// <param name="manufacturer">制造商</param>
    /// <param name="model">设备型号</param>
    /// <param name="serialNumber">序列号</param>
    /// <param name="dataCollectionInterval">数据采集间隔</param>
    /// <param name="enableAlarmCollection">是否启用报警采集</param>
    /// <param name="updatedBy">更新者</param>
    public void UpdateBasicInfo(
        string? manufacturer = null,
        string? model = null,
        string? serialNumber = null,
        int? dataCollectionInterval = null,
        bool? enableAlarmCollection = null,
        string? updatedBy = null)
    {
        var hasChanges = false;
        var changes = new List<string>();

        if (manufacturer != null && Manufacturer != manufacturer.Trim())
        {
            var oldValue = Manufacturer;
            Manufacturer = manufacturer.Trim();
            changes.Add($"Manufacturer: {oldValue} → {Manufacturer}");
            hasChanges = true;
        }

        if (model != null && Model != model.Trim())
        {
            var oldValue = Model;
            Model = model.Trim();
            changes.Add($"Model: {oldValue} → {Model}");
            hasChanges = true;
        }

        if (serialNumber != null && SerialNumber != serialNumber.Trim())
        {
            var oldValue = SerialNumber;
            SerialNumber = serialNumber.Trim();
            changes.Add($"SerialNumber: {oldValue} → {SerialNumber}");
            hasChanges = true;
        }

        if (dataCollectionInterval.HasValue && DataCollectionInterval != dataCollectionInterval.Value)
        {
            var oldValue = DataCollectionInterval;
            DataCollectionInterval = dataCollectionInterval.Value;
            changes.Add($"DataCollectionInterval: {oldValue} → {DataCollectionInterval}");
            hasChanges = true;
        }

        if (enableAlarmCollection.HasValue && EnableAlarmCollection != enableAlarmCollection.Value)
        {
            var oldValue = EnableAlarmCollection;
            EnableAlarmCollection = enableAlarmCollection.Value;
            changes.Add($"EnableAlarmCollection: {oldValue} → {EnableAlarmCollection}");
            hasChanges = true;
        }

        if (hasChanges)
        {
            UpdatedBy = updatedBy;
            UpdatedAt = DateTime.UtcNow;

            // 发布设备信息更新事件
            AddDomainEvent(new EquipmentBasicInfoUpdatedEvent(Id, changes, updatedBy, UpdatedAt));
        }
    }

    #region 连接管理业务方法

    /// <summary>
    /// 建立设备连接
    /// </summary>
    /// <param name="sessionId">连接会话ID</param>
    /// <param name="connectedBy">连接操作者</param>
    public void Connect(string sessionId, string? connectedBy = null)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("Session ID cannot be null or empty", nameof(sessionId));

        if (ConnectionState.IsConnected)
        {
            throw new InvalidOperationException($"Equipment {Id} is already connected with session {ConnectionState.SessionId}");
        }

        // 检查设备是否可以连接
        if (!CanConnect())
        {
            throw new InvalidOperationException($"Equipment {Id} cannot connect in current state: {State}");
        }

        var previousState = ConnectionState;
        var connectedAt = DateTime.UtcNow;

        // 使用正确的静态方法调用
        ConnectionState = ConnectionState.Connected(sessionId, connectedAt);
        LastHeartbeat = connectedAt;
        UpdatedAt = connectedAt;
        UpdatedBy = connectedBy;

        // 如果是从未知状态连接，设置为空闲状态
        if (State == EquipmentState.UNKNOWN)
        {
            UpdateState(EquipmentState.IDLE, "Connected to equipment", connectedBy);
        }

        // 发布连接成功事件
        var isReconnection = previousState.RequiresReconnection;
        AddDomainEvent(new EquipmentConnectedEvent(
            Id, connectedAt, sessionId, Configuration.Endpoint,
            isReconnection: isReconnection));
    }


    /// <summary>
    /// 断开设备连接
    /// </summary>
    /// <param name="reason">断开原因</param>
    /// <param name="disconnectedBy">断开操作者</param>
    /// <param name="isExpected">是否为预期断开</param>
    public void Disconnect(string? reason = null, string? disconnectedBy = null, bool isExpected = false)
    {
        if (!ConnectionState.IsConnected)
        {
            return; // 已经断开，无需处理
        }

        var previousSessionId = ConnectionState.SessionId;
        var connectionDuration = ConnectionState.ConnectionDuration;
        var disconnectedAt = DateTime.UtcNow;

        // 修复：使用正确的静态方法调用
        ConnectionState = ConnectionState.Disconnect(reason, disconnectedAt);
        LastHeartbeat = null;
        UpdatedAt = disconnectedAt;
        UpdatedBy = disconnectedBy;

        // 如果是意外断开且设备在执行状态，标记为故障
        if (!isExpected && State == EquipmentState.EXECUTING)
        {
            UpdateState(EquipmentState.FAULT, $"Unexpected disconnection: {reason}", disconnectedBy);
        }

        // 发布断开连接事件
        var disconnectionType = DetermineDisconnectionType(reason, isExpected);
        AddDomainEvent(new EquipmentDisconnectedEvent(
            Id, disconnectedAt, reason, disconnectionType,
            previousSessionId, connectionDuration, State,
            isExpected, !isExpected));
    }

    /// <summary>
    /// 更新心跳
    /// </summary>
    public void UpdateHeartbeat()
    {
        if (!ConnectionState.IsConnected)
        {
            throw new InvalidOperationException($"Cannot update heartbeat for disconnected equipment {Id}");
        }

        var now = DateTime.UtcNow;
        ConnectionState = ConnectionState.UpdateHeartbeat();
        LastHeartbeat = now;
        UpdatedAt = now;

        // 检查是否从不稳定状态恢复
        if (!HealthStatus.IsHealthy() && ConnectionState.IsStable)
        {
            HealthStatus = HealthStatus.Healthy;
            AddDomainEvent(new EquipmentHealthRecoveredEvent(Id, HealthStatus, now));
        }
    }


    /// <summary>
    /// 检查连接能力
    /// </summary>
    /// <returns>是否可以连接</returns>
    public bool CanConnect()
    {
        return State != EquipmentState.MAINTENANCE &&
               !ConnectionState.IsConnected &&
               Configuration.IsValid;
    }

    #endregion

    #region 状态管理业务方法

    /// <summary>
    /// 更新设备状态
    /// </summary>
    /// <param name="newState">新状态</param>
    /// <param name="reason">状态变化原因</param>
    /// <param name="changedBy">状态变化操作者</param>
    /// <param name="subState">子状态</param>
    public void UpdateState(EquipmentState newState, string? reason = null, string? changedBy = null, string? subState = null)
    {
        if (State == newState && SubState == subState)
            return; // 状态无变化

        var previousState = State;
        var previousSubState = SubState;
        var changedAt = DateTime.UtcNow;

        State = newState;
        SubState = subState;
        UpdatedAt = changedAt;
        UpdatedBy = changedBy;

        // 更新健康状态
        UpdateHealthStatus();

        // 使用正确的事件名称 EquipmentStatusChangedEvent
        AddDomainEvent(new EquipmentStatusChangedEvent(
            Id, previousState, newState, reason, changedAt, changedBy));

        // 如果状态变为严重状态，检查是否需要额外处理
        if (newState.RequiresAttention())
        {
            HandleCriticalStateChange(previousState, newState, reason);
        }
    }

    /// <summary>
    /// 更新健康状态
    /// </summary>
    private void UpdateHealthStatus()
    {
        var previousHealth = HealthStatus;

        HealthStatus = State switch
        {
            EquipmentState.FAULT => HealthStatus.Unhealthy,
            EquipmentState.ALARM when _activeAlarms.Any(a => a.Severity >= AlarmSeverity.MAJOR) => HealthStatus.Degraded,
            EquipmentState.IDLE or EquipmentState.EXECUTING =>
                ConnectionState.IsConnected ? HealthStatus.Healthy : HealthStatus.Degraded,
            EquipmentState.MAINTENANCE => HealthStatus.Degraded,
            _ => HealthStatus.Unknown
        };

        if (HealthStatus != previousHealth)
        {
            AddDomainEvent(new EquipmentHealthChangedEvent(Id, previousHealth, HealthStatus, DateTime.UtcNow));
        }
    }

    /// <summary>
    /// 验证状态转换合法性
    /// </summary>
    private bool IsValidStateTransition(EquipmentState fromState, EquipmentState toState)
    {
        // UNKNOWN状态可以转换到任何状态
        if (fromState == EquipmentState.UNKNOWN)
            return true;

        // 基本的状态转换规则
        return (fromState, toState) switch
        {
            // 从IDLE可以转换到大部分状态
            (EquipmentState.IDLE, _) => toState != EquipmentState.UNKNOWN,

            // 从SETUP可以转换到执行或回到空闲
            (EquipmentState.SETUP, EquipmentState.EXECUTING or EquipmentState.IDLE or EquipmentState.FAULT or EquipmentState.ALARM) => true,

            // 从EXECUTING可以转换到暂停、完成（空闲）或故障
            (EquipmentState.EXECUTING, EquipmentState.PAUSE or EquipmentState.IDLE or EquipmentState.FAULT or EquipmentState.ALARM) => true,

            // 从PAUSE可以转换到继续执行或停止
            (EquipmentState.PAUSE, EquipmentState.EXECUTING or EquipmentState.IDLE or EquipmentState.FAULT) => true,

            // 从DOWN可以转换到维护或恢复到空闲
            (EquipmentState.DOWN, EquipmentState.MAINTENANCE or EquipmentState.IDLE) => true,

            // 从MAINTENANCE可以转换到空闲
            (EquipmentState.MAINTENANCE, EquipmentState.IDLE) => true,

            // 从FAULT可以转换到维护或恢复到空闲
            (EquipmentState.FAULT, EquipmentState.MAINTENANCE or EquipmentState.IDLE or EquipmentState.DOWN) => true,

            // 从ALARM可以转换回之前的状态或故障
            (EquipmentState.ALARM, _) => toState != EquipmentState.UNKNOWN,

            _ => false
        };
    }

    /// <summary>
    /// 确定状态变化类型
    /// </summary>
    private StateChangeType DetermineChangeType(EquipmentState previousState, EquipmentState newState, string? changedBy)
    {
        if (newState == EquipmentState.FAULT)
            return StateChangeType.Error;

        if (newState == EquipmentState.ALARM)
            return StateChangeType.AlarmTriggered;

        if (newState == EquipmentState.MAINTENANCE || previousState == EquipmentState.MAINTENANCE)
            return StateChangeType.Maintenance;

        if (!string.IsNullOrEmpty(changedBy))
            return StateChangeType.OperatorTriggered;

        return StateChangeType.Automatic;
    }

    /// <summary>
    /// 处理严重状态变化
    /// </summary>
    private void HandleCriticalStateChange(EquipmentState previousState, EquipmentState newState, string? reason)
    {
        // 如果进入故障状态，清除所有挂起的命令
        if (newState == EquipmentState.FAULT)
        {
            CancelPendingCommands("Equipment in fault state");
        }

        // 发布需要关注的设备状态事件
        AddDomainEvent(new EquipmentRequiresAttentionEvent(Id, newState, reason, DateTime.UtcNow));
    }

    #endregion

    #region 报警管理业务方法

    /// <summary>
    /// 添加报警
    /// </summary>
    /// <param name="alarmId">报警ID</param>
    /// <param name="alarmText">报警文本</param>
    /// <param name="severity">严重程度</param>
    /// <param name="additionalData">附加数据</param>
    public void AddAlarm(ushort alarmId, string alarmText, AlarmSeverity severity, IDictionary<string, object>? additionalData = null)
    {
        if (string.IsNullOrWhiteSpace(alarmText))
            throw new ArgumentException("Alarm text cannot be null or empty", nameof(alarmText));

        // 检查报警是否已存在
        if (_activeAlarms.Any(a => a.AlarmId == alarmId))
        {
            return; // 报警已存在，忽略重复添加
        }

        var now = DateTime.UtcNow;

        var alarm = new AlarmEvent(
            Id.Value,           //  使用 EquipmentId.Value 转换为字符串
            alarmId,            //  alarmId 参数
            null,               //  alarmCode (可选)
            alarmText,          //  alarmText 参数
            severity,           //  severity 参数
            now                 //  setTime 参数
        )
        {
            AdditionalData = additionalData  //  设置附加数据
        };

        _activeAlarms.Add(alarm);
        UpdatedAt = now;

        // 如果是严重报警，可能需要状态变更
        if (severity >= AlarmSeverity.MAJOR && State != EquipmentState.ALARM && State != EquipmentState.FAULT)
        {
            UpdateState(EquipmentState.ALARM, $"Major alarm triggered: {alarmText}");
        }

        // 更新健康状态
        UpdateHealthStatus(HealthStatus);

        // 发布报警触发事件
        AddDomainEvent(new AlarmTriggeredEvent(Id, alarm, now));
    }

    /// <summary>
    /// 清除报警
    /// </summary>
    /// <param name="alarmId">报警ID</param>
    /// <param name="reason">清除原因</param>
    /// <param name="clearedBy">清除操作者</param>
    public void ClearAlarm(ushort alarmId, string? reason = null, string? clearedBy = null)
    {
        var alarm = _activeAlarms.FirstOrDefault(a => a.AlarmId == alarmId);
        if (alarm is null)  // 修复：使用 is null
            return; // 报警不存在

        _activeAlarms.Remove(alarm);
        var now = DateTime.UtcNow;
        UpdatedAt = now;
        UpdatedBy = clearedBy;

        // 修复：创建清除状态的报警副本
        var clearedAlarm = alarm.CreateClearedCopy(reason, clearedBy);

        // 检查是否还有活动的严重报警
        var hasMajorAlarms = _activeAlarms.Any(a => a.Severity >= AlarmSeverity.MAJOR);
        if (!hasMajorAlarms && State == EquipmentState.ALARM)
        {
            UpdateState(EquipmentState.IDLE, "All major alarms cleared", clearedBy);
        }

        // 更新健康状态
        UpdateHealthStatus();

        // 发布报警清除事件
        AddDomainEvent(new AlarmClearedEvent(Id, clearedAlarm, reason, now, clearedBy));
    }


    /// <summary>
    /// 确认报警
    /// </summary>
    /// <param name="alarmId">报警ID</param>
    /// <param name="acknowledgedBy">确认者</param>
    public void AcknowledgeAlarm(ushort alarmId, string acknowledgedBy)
    {
        if (string.IsNullOrWhiteSpace(acknowledgedBy))
            throw new ArgumentException("Acknowledged by cannot be null or empty", nameof(acknowledgedBy));

        var alarm = _activeAlarms.FirstOrDefault(a => a.AlarmId == alarmId);
        if (alarm is null)  // 修复：使用 is null
            throw new InvalidOperationException($"Alarm {alarmId} not found for equipment {Id}");

        if (alarm.AcknowledgedAt.HasValue)
            return; // 已确认

        // 修复：直接修改属性，不使用 with 表达式
        alarm.AcknowledgedBy = acknowledgedBy;
        alarm.AcknowledgedAt = DateTime.UtcNow;
        alarm.UpdatedAt = DateTime.UtcNow;

        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = acknowledgedBy;

        // 发布报警确认事件
        AddDomainEvent(new AlarmAcknowledgedEvent(Id, alarmId, acknowledgedBy, DateTime.UtcNow));
    }

    #endregion

    #region 数据管理业务方法

    /// <summary>
    /// 添加追踪数据
    /// </summary>
    /// <param name="reportId">报告ID</param>
    /// <param name="dataValues">数据值</param>
    /// <param name="timestamp">时间戳</param>
    public void AddTraceData(int reportId, IDictionary<string, object> dataValues, DateTime? timestamp = null)
    {
        if (dataValues == null || !dataValues.Any())
            throw new ArgumentException("Data values cannot be null or empty", nameof(dataValues));

        var now = timestamp ?? DateTime.UtcNow;

        // 修复：使用静态工厂方法创建 TraceData
        var traceData = TraceData.Create(
            Id.Value,           // 使用 EquipmentId.Value 转换为字符串
            reportId,           // reportId
            dataValues,         // dataValues
            now                 // timestamp
        );

        _recentTraceData.Add(traceData);

        // 保持最近100条数据
        while (_recentTraceData.Count > 100)
        {
            _recentTraceData.RemoveAt(0);
        }

        //使用正确的方法名
        Metrics = Metrics.IncrementDataCount();
        LastDataUpdate = now;
        UpdatedAt = DateTime.UtcNow;

        // 更新健康状态
        UpdateHealthStatus();

        var dataVariables = dataValues
       .Where(kvp => uint.TryParse(kvp.Key, out _))
       .ToDictionary(kvp => uint.Parse(kvp.Key), kvp => kvp.Value)
       .AsReadOnly();

        AddDomainEvent(new TraceDataReceivedEvent(Id, dataVariables, now));
    }

    /// <summary>
    /// 获取最新的数据值
    /// </summary>
    /// <param name="variableNames">变量名列表</param>
    /// <returns>最新的数据值字典</returns>
    public IDictionary<string, object> GetLatestDataValues(IEnumerable<string>? variableNames = null)
    {
        var latestData = _recentTraceData.OrderByDescending(d => d.Timestamp).FirstOrDefault();
        if (latestData?.DataValues == null)
            return new Dictionary<string, object>();

        if (variableNames == null)
            return new Dictionary<string, object>(latestData.DataValues);

        var requestedVariables = variableNames.ToHashSet();
        return latestData.DataValues
            .Where(kvp => requestedVariables.Contains(kvp.Key))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    #endregion

    #region 远程控制业务方法 - 改进版本

    /// <summary>
    /// 发送远程命令（别名方法，保持向后兼容）
    /// </summary>
    /// <param name="commandName">命令名称</param>
    /// <param name="parameters">命令参数</param>
    /// <param name="requestedBy">请求者</param>
    /// <param name="timeoutSeconds">超时时间（秒）</param>
    /// <returns>命令ID</returns>
    public Guid SendRemoteCommand(
        string commandName,
        IDictionary<string, object>? parameters = null,
        string? requestedBy = null,
        int timeoutSeconds = 30)
    {
        // 委托给ExecuteRemoteCommand方法
        return ExecuteRemoteCommand(commandName, parameters, requestedBy, timeoutSeconds);
    }

    /// <summary>
    /// 发送远程命令（重载方法，支持Dictionary类型参数）
    /// </summary>
    /// <param name="commandName">命令名称</param>
    /// <param name="parameters">命令参数</param>
    /// <param name="requestedBy">请求者</param>
    /// <param name="timeoutSeconds">超时时间（秒）</param>
    /// <returns>命令ID</returns>
    public Guid SendRemoteCommand(
        string commandName,
        Dictionary<string, object>? parameters = null,
        string? requestedBy = null,
        int timeoutSeconds = 30)
    {
        // 将Dictionary转换为IDictionary并委托给ExecuteRemoteCommand
        IDictionary<string, object>? paramDict = parameters;
        return ExecuteRemoteCommand(commandName, paramDict, requestedBy, timeoutSeconds);
    }

    /// <summary>
    /// 执行远程命令（核心实现）
    /// </summary>
    /// <param name="commandName">命令名称</param>
    /// <param name="parameters">命令参数</param>
    /// <param name="requestedBy">请求者</param>
    /// <param name="timeoutSeconds">超时时间（秒）</param>
    /// <returns>命令ID</returns>
    public Guid ExecuteRemoteCommand(
        string commandName,
        IDictionary<string, object>? parameters = null,
        string? requestedBy = null,
        int timeoutSeconds = 30)
    {
        if (string.IsNullOrWhiteSpace(commandName))
            throw new ArgumentException("Command name cannot be null or empty", nameof(commandName));

        if (!CanExecuteCommand(commandName))
            throw new InvalidOperationException($"Cannot execute command '{commandName}' in current state: {State}");

        var commandId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        // 序列化参数为 JSON 字符串
        string? parametersJson = null;
        if (parameters != null && parameters.Any())
        {
            try
            {
                parametersJson = JsonSerializer.Serialize(parameters, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false
                });
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Failed to serialize parameters: {ex.Message}", nameof(parameters));
            }
        }

        var command = new RemoteCommand
        {
            Id = commandId,
            EquipmentId = Id.Value,
            CommandName = commandName,
            Parameters = parametersJson,
            Status = CommandStatus.Pending,
            RequestedBy = requestedBy,
            RequestedAt = now,
            TimeoutAt = now.AddSeconds(timeoutSeconds)
        };

        _commandHistory.Add(command);

        // 保持最近50条命令历史
        while (_commandHistory.Count > 50)
        {
            _commandHistory.RemoveAt(0);
        }

        UpdatedAt = now;
        UpdatedBy = requestedBy;

        // 发布远程命令事件
        AddDomainEvent(new RemoteCommandRequestedEvent(Id, command, now));

        return commandId;
    }

    /// <summary>
    /// 更新命令状态
    /// </summary>
    /// <param name="commandId">命令ID</param>
    /// <param name="status">新状态</param>
    /// <param name="resultMessage">结果消息</param>
    /// <param name="resultData">结果数据</param>
    public void UpdateCommandStatus(
        Guid commandId,
        CommandStatus status,
        string? resultMessage = null,
        IDictionary<string, object>? resultData = null)
    {
        var command = _commandHistory.FirstOrDefault(c => c.Id == commandId);
        if (command == null)
            return; // 命令不存在

        var now = DateTime.UtcNow;
        var index = _commandHistory.IndexOf(command);

        _commandHistory[index] = command with
        {
            Status = status,
            ResultMessage = resultMessage,
            ResultData = resultData != null ? new ReadOnlyDictionary<string, object>(resultData) : null,
            CompletedAt = status == CommandStatus.Completed || status == CommandStatus.Failed ? now : null
        };

        UpdatedAt = now;

        // 发布命令状态更新事件
        AddDomainEvent(new RemoteCommandStatusUpdatedEvent(Id, commandId, status, resultMessage, now));
    }

    /// <summary>
    /// 检查是否可以执行命令
    /// </summary>
    /// <param name="commandName">命令名称</param>
    /// <returns>是否可以执行</returns>
    public bool CanExecuteCommand(string commandName)
    {
        if (!ConnectionState.IsConnected)
            return false;

        if (State == EquipmentState.FAULT || State == EquipmentState.MAINTENANCE)
            return false;

        // 根据命令类型和当前状态判断
        return commandName.ToUpperInvariant() switch
        {
            "START" => State == EquipmentState.IDLE || State == EquipmentState.SETUP,
            "STOP" => State == EquipmentState.EXECUTING || State == EquipmentState.PAUSE,
            "PAUSE" => State == EquipmentState.EXECUTING,
            "RESUME" => State == EquipmentState.PAUSE,
            "ABORT" => State != EquipmentState.IDLE,
            "RESET" => State == EquipmentState.FAULT || State == EquipmentState.ALARM,
            _ => true // 其他命令默认允许
        };
    }

    /// <summary>
    /// 取消挂起的命令
    /// </summary>
    /// <param name="reason">取消原因</param>
    private void CancelPendingCommands(string reason)
    {
        var pendingCommands = _commandHistory.Where(c => c.Status == CommandStatus.Pending).ToList();

        foreach (var command in pendingCommands)
        {
            UpdateCommandStatus(command.Id, CommandStatus.Cancelled, reason);
        }
    }

    #endregion

    #region 健康检查和诊断

    /// <summary>
    /// 确定断开连接类型
    /// </summary>
    private static DisconnectionType DetermineDisconnectionType(string? reason, bool isExpected)
    {
        if (isExpected)
            return DisconnectionType.Manual;

        if (reason?.Contains("timeout", StringComparison.OrdinalIgnoreCase) == true)
            return DisconnectionType.Timeout;

        if (reason?.Contains("network", StringComparison.OrdinalIgnoreCase) == true)
            return DisconnectionType.NetworkError;

        return DisconnectionType.Unexpected;
    }

    /// <summary>
    /// 执行健康检查
    /// </summary>
    /// <returns>健康检查结果</returns>
    public HealthCheckResult PerformHealthCheck()
    {
        var issues = new List<string>();
        var now = DateTime.UtcNow;

        // 检查连接状态
        if (!ConnectionState.IsConnected)
        {
            issues.Add("Equipment is not connected");
        }
        else if (!ConnectionState.IsStable)
        {
            issues.Add("Connection is unstable");
        }

        // 检查心跳
        if (LastHeartbeat.HasValue && now - LastHeartbeat.Value > TimeSpan.FromMinutes(2))
        {
            issues.Add("Heartbeat timeout detected");
        }

        // 检查活动报警
        var criticalAlarms = _activeAlarms.Where(a => a.Severity >= AlarmSeverity.MAJOR).ToList();
        if (criticalAlarms.Any())
        {
            issues.Add($"{criticalAlarms.Count} critical alarm(s) active");
        }

        // 检查数据更新
        if (Configuration.EnableDataCollection && LastDataUpdate.HasValue &&
            now - LastDataUpdate.Value > TimeSpan.FromMinutes(5))
        {
            issues.Add("No recent data updates");
        }

        // 检查设备状态
        if (State.RequiresAttention())
        {
            issues.Add($"Equipment in {State.GetDisplayName()} state");
        }

        // 确定健康状态
        var healthStatus = issues.Any() ?
            (issues.Count > 2 || criticalAlarms.Any() ? HealthStatus.Unhealthy : HealthStatus.Degraded) :
            HealthStatus.Healthy;

        return new HealthCheckResult(Id, healthStatus, issues, now);
    }

    ///// <summary>
    ///// 更新健康状态
    ///// </summary>
    //private void UpdateHealthStatus()
    //{
    //    var healthCheck = PerformHealthCheck();
    //    var previousStatus = HealthStatus;
    //    HealthStatus = healthCheck.Status;

    //    // 如果健康状态发生变化，发布事件
    //    if (previousStatus != HealthStatus)
    //    {
    //        if (HealthStatus == HealthStatus.Healthy)
    //        {
    //            AddDomainEvent(new EquipmentHealthRecoveredEvent(Id, HealthStatus, DateTime.UtcNow));
    //        }
    //        else
    //        {
    //            AddDomainEvent(new EquipmentHealthDegradedEvent(Id, HealthStatus, healthCheck.Issues, DateTime.UtcNow));
    //        }
    //    }
    //}

    /// <summary>
    /// 更新健康状态
    /// </summary>
    /// <param name="healthStatus">健康状态</param>
    /// <param name="updatedBy">更新者</param>
    public void UpdateHealthStatus(HealthStatus healthStatus, string? updatedBy = null)
    {
        if (HealthStatus == healthStatus)
            return;

        HealthStatus = healthStatus;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }

    /// <summary>
    /// 获取设备诊断信息
    /// </summary>
    /// <returns>诊断信息</returns>
    public EquipmentDiagnostics GetDiagnostics()
    {
        var now = DateTime.UtcNow;
        var uptime = ConnectionState.ConnectionDuration;

        return new EquipmentDiagnostics
        {
            EquipmentId = Id,
            CurrentState = State,
            ConnectionState = ConnectionState,
            HealthStatus = HealthStatus,
            ActiveAlarmCount = _activeAlarms.Count,
            CriticalAlarmCount = _activeAlarms.Count(a => a.Severity >= AlarmSeverity.MAJOR),
            RecentDataCount = _recentTraceData.Count,
            PendingCommandCount = _commandHistory.Count(c => c.Status == CommandStatus.Pending),
            Uptime = uptime,
            LastHeartbeat = LastHeartbeat,
            LastDataUpdate = LastDataUpdate,
            Metrics = Metrics,
            DiagnosticTime = now
        };
    }

    #endregion

    #region 配置管理

    /// <summary>
    /// 更新设备配置
    /// </summary>
    /// <param name="newConfiguration">新配置</param>
    /// <param name="updatedBy">更新者</param>
    public void UpdateConfiguration(EquipmentConfiguration newConfiguration, string? updatedBy = null)
    {
        if (newConfiguration == null)
            throw new ArgumentNullException(nameof(newConfiguration));

        if (!newConfiguration.IsValid)
            throw new ArgumentException("Configuration is invalid", nameof(newConfiguration));

        var oldConfiguration = Configuration;
        var now = DateTime.UtcNow;

        Configuration = newConfiguration;
        UpdatedAt = now;
        UpdatedBy = updatedBy;

        // 如果网络端点发生变化且当前已连接，需要断开重连
        if (!oldConfiguration.Endpoint.Equals(newConfiguration.Endpoint) && ConnectionState.IsConnected)
        {
            Disconnect("Configuration endpoint changed", updatedBy, true);
        }

        // 发布配置变更事件
        AddDomainEvent(new EquipmentConfigurationChangedEvent(Id, oldConfiguration, newConfiguration, updatedBy, now));
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 检查消息发送能力
    /// </summary>
    /// <returns>是否可以发送消息</returns>
    public bool CanSendMessage()
    {
        return ConnectionState.IsConnected &&
               ConnectionState.IsStable &&
               State != EquipmentState.FAULT &&
               State != EquipmentState.MAINTENANCE;
    }

    /// <summary>
    /// 获取设备摘要信息
    /// </summary>
    /// <returns>设备摘要</returns>
    public EquipmentSummary GetSummary()
    {
        return new EquipmentSummary
        {
            Id = Id,
            Name = Name,
            State = State,
            SubState = SubState,
            ConnectionState = ConnectionState,
            HealthStatus = HealthStatus,
            ActiveAlarmCount = _activeAlarms.Count,
            CriticalAlarmCount = _activeAlarms.Count(a => a.Severity >= AlarmSeverity.MAJOR),
            LastHeartbeat = LastHeartbeat,
            LastDataUpdate = LastDataUpdate,
            UpdatedAt = UpdatedAt
        };
    }

    #endregion

    public override string ToString()
    {
        var connectionStatus = ConnectionState.IsConnected ? "Connected" : "Disconnected";
        var alarmInfo = _activeAlarms.Any() ? $", {_activeAlarms.Count} alarms" : "";
        return $"Equipment {Id} [{Name}] - {State} ({connectionStatus}){alarmInfo}";
    }
}

#region 相关实体和值对象定义


/// <summary>
/// 健康状态扩展方法
/// </summary>
public static class HealthStatusExtensions
{
    public static bool IsHealthy(this HealthStatus status) => status == HealthStatus.Healthy;
    public static bool RequiresAttention(this HealthStatus status) => status >= HealthStatus.Degraded;

    public static string GetDisplayName(this HealthStatus status) => status switch
    {
        HealthStatus.Unknown => "Unknown",
        HealthStatus.Healthy => "Healthy",
        HealthStatus.Degraded => "Degraded",
        HealthStatus.Unhealthy => "Unhealthy",
        _ => status.ToString()
    };
}

/// <summary>
/// 健康检查结果
/// </summary>
public record HealthCheckResult
{
    public EquipmentId EquipmentId { get; init; }
    public HealthStatus Status { get; init; }
    public IReadOnlyList<string> Issues { get; init; }
    public DateTime CheckTime { get; init; }

    public HealthCheckResult(EquipmentId equipmentId, HealthStatus status, IEnumerable<string> issues, DateTime checkTime)
    {
        EquipmentId = equipmentId;
        Status = status;
        Issues = issues.ToList().AsReadOnly();
        CheckTime = checkTime;
    }
}

/// <summary>
/// 设备诊断信息
/// </summary>
public record EquipmentDiagnostics
{
    public EquipmentId? EquipmentId { get; init; }
    public EquipmentState CurrentState { get; init; }
    public ConnectionState? ConnectionState { get; init; }
    public HealthStatus HealthStatus { get; init; }
    public int ActiveAlarmCount { get; init; }
    public int CriticalAlarmCount { get; init; }
    public int RecentDataCount { get; init; }
    public int PendingCommandCount { get; init; }
    public TimeSpan? Uptime { get; init; }
    public DateTime? LastHeartbeat { get; init; }
    public DateTime? LastDataUpdate { get; init; }
    public ProcessingMetrics? Metrics { get; init; }
    public DateTime DiagnosticTime { get; init; }
}

/// <summary>
/// 设备摘要信息
/// </summary>
public record EquipmentSummary
{
    public EquipmentId? Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public EquipmentState State { get; init; }
    public string? SubState { get; init; }
    public ConnectionState? ConnectionState { get; init; }
    public HealthStatus HealthStatus { get; init; }
    public int ActiveAlarmCount { get; init; }
    public int CriticalAlarmCount { get; init; }
    public DateTime? LastHeartbeat { get; init; }
    public DateTime? LastDataUpdate { get; init; }
    public DateTime UpdatedAt { get; init; }
}

#endregion
