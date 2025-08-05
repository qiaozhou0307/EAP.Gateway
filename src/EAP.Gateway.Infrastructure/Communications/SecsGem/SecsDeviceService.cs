using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using EAP.Gateway.Core.Entities;
using EAP.Gateway.Core.Events.Alarm;
using EAP.Gateway.Core.Events.Equipment;
using EAP.Gateway.Core.Models;
using EAP.Gateway.Core.Repositories;
using EAP.Gateway.Core.Services;
using EAP.Gateway.Core.ValueObjects;
using EAP.Gateway.Infrastructure.Communications.SecsGem.Events;
using MediatR;
using Microsoft.Extensions.Logging;
using Secs4Net;

namespace EAP.Gateway.Infrastructure.Communications.SecsGem;

/// <summary>
/// SECS设备服务实现
/// 整合HsmsClient和Equipment聚合根，实现完整的设备管理功能
/// 负责设备通信、状态同步、数据采集、报警处理、远程控制等核心业务
/// </summary>
public class SecsDeviceService : ISecsDeviceService
{
    private readonly EquipmentId _equipmentId;
    private readonly IHsmsClient _hsmsClient;
    private readonly ILogger<SecsDeviceService> _logger;
    private readonly IMediator _mediator;

    private Equipment? _equipment;
    private DeviceServiceStatus _status = DeviceServiceStatus.NotInitialized;
    private readonly SemaphoreSlim _statusSemaphore = new(1, 1);
    private readonly SemaphoreSlim _operationSemaphore = new(1, 1);
    private readonly CancellationTokenSource _serviceCts = new();

    // 数据采集相关
    private readonly ConcurrentDictionary<uint, DateTime> _activeDataVariables = new();
    private readonly ConcurrentDictionary<uint, DateTime> _activeEvents = new();
    private readonly ConcurrentDictionary<uint, DateTime> _activeAlarms = new();

    // 任务管理
    private Task? _dataCollectionTask;
    private Task? _healthMonitoringTask;
    private Task? _messageProcessingTask;

    // 统计信息
    private int _dataMessagesReceived = 0;
    private int _alarmMessagesReceived = 0;
    private int _controlMessagesReceived = 0;
    private DateTime _serviceStartedAt = DateTime.UtcNow;

    private volatile bool _disposed = false;
    private volatile bool _dataCollectionEnabled = false;
    private volatile bool _alarmHandlingEnabled = false;

    public EquipmentId EquipmentId => _equipmentId;
    public Equipment? Equipment => _equipment;
    public IHsmsClient HsmsClient => _hsmsClient;
    public bool IsStarted => _status == DeviceServiceStatus.Started;
    public bool IsStopped => _status == DeviceServiceStatus.Stopped;
    public bool IsOnline => IsStarted && _hsmsClient.IsConnected && _equipment?.State.IsAvailable() == true;
    public HealthStatus HealthStatus => _equipment?.HealthStatus ?? HealthStatus.Unknown;

    public event EventHandler<DeviceServiceStatusChangedEventArgs>? StatusChanged;
    public event EventHandler<DeviceDataReceivedEventArgs>? DataReceived;
    public event EventHandler<DeviceAlarmEventArgs>? AlarmEvent;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="equipmentId">设备标识</param>
    /// <param name="hsmsClient">HSMS客户端</param>
    /// <param name="mediator">中介者</param>
    /// <param name="logger">日志记录器</param>
    public SecsDeviceService(
        EquipmentId equipmentId,
        IHsmsClient hsmsClient,
        IMediator mediator,
        ILogger<SecsDeviceService> logger)
    {
        _equipmentId = equipmentId ?? throw new ArgumentNullException(nameof(equipmentId));
        _hsmsClient = hsmsClient ?? throw new ArgumentNullException(nameof(hsmsClient));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // 订阅HSMS客户端事件
        _hsmsClient.ConnectionStateChanged += OnConnectionStateChanged;
        _hsmsClient.MessageReceived += OnMessageReceived;
        _hsmsClient.MessageTimeout += OnMessageTimeout;

        _logger.LogInformation("SECS设备服务已创建 [设备ID: {EquipmentId}]", _equipmentId);
    }

    #region 生命周期管理

    /// <summary>
    /// 启动设备服务
    /// </summary>
    public async Task StartAsync(Equipment equipment, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (equipment == null)
            throw new ArgumentNullException(nameof(equipment));

        if (equipment.Id != _equipmentId)
            throw new ArgumentException($"设备ID不匹配: 期望 {_equipmentId}, 实际 {equipment.Id}");

        await _statusSemaphore.WaitAsync(cancellationToken);
        try
        {
            if (_status == DeviceServiceStatus.Started)
            {
                _logger.LogWarning("设备服务 {EquipmentId} 已启动，跳过启动操作", _equipmentId);
                return;
            }

            await ChangeStatusAsync(DeviceServiceStatus.Starting, "启动设备服务");

            _logger.LogInformation("启动设备服务 [设备ID: {EquipmentId}, 名称: {Name}]",
                _equipmentId, equipment.Name);

            // 设置设备聚合根
            _equipment = equipment;
            _serviceStartedAt = DateTime.UtcNow;

            // 连接设备
            var connected = await ConnectInternalAsync(cancellationToken);
            if (!connected)
            {
                await ChangeStatusAsync(DeviceServiceStatus.Faulted, "设备连接失败");
                throw new InvalidOperationException($"设备 {_equipmentId} 连接失败");
            }

            // 启动后台任务
            _messageProcessingTask = ProcessMessagesAsync(_serviceCts.Token);
            _healthMonitoringTask = MonitorHealthAsync(_serviceCts.Token);

            // 如果配置启用了数据采集，自动启动
            if (_equipment.Configuration.EnableDataCollection)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        // 延迟启动数据采集，确保连接稳定
                        await Task.Delay(2000, cancellationToken);
                        await StartDataCollectionAsync(GetDefaultDataVariables(), GetDefaultEvents(), cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "自动启动数据采集失败 [设备: {EquipmentId}]: {ErrorMessage}",
                            _equipmentId, ex.Message);
                    }
                });
            }

            // 如果配置启用了报警处理，自动启动
            if (_equipment.Configuration.EnableAlarmHandling)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(2000, cancellationToken);
                        await EnableAlarmHandlingAsync(GetDefaultAlarmIds(), cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "自动启动报警处理失败 [设备: {EquipmentId}]: {ErrorMessage}",
                            _equipmentId, ex.Message);
                    }
                });
            }

            await ChangeStatusAsync(DeviceServiceStatus.Started, "设备服务启动完成");

            _logger.LogInformation("设备服务启动成功 [设备ID: {EquipmentId}]", _equipmentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "启动设备服务失败 [设备: {EquipmentId}]: {ErrorMessage}", _equipmentId, ex.Message);
            await ChangeStatusAsync(DeviceServiceStatus.Faulted, $"启动失败: {ex.Message}");
            throw;
        }
        finally
        {
            _statusSemaphore.Release();
        }
    }

    /// <summary>
    /// 停止设备服务
    /// </summary>
    public async Task StopAsync(string? reason = null, CancellationToken cancellationToken = default)
    {
        if (_disposed || _status == DeviceServiceStatus.Stopped)
        {
            return;
        }

        await _statusSemaphore.WaitAsync(cancellationToken);
        try
        {
            await ChangeStatusAsync(DeviceServiceStatus.Stopping, reason ?? "停止设备服务");

            _logger.LogInformation("停止设备服务 [设备ID: {EquipmentId}, 原因: {Reason}]",
                _equipmentId, reason ?? "手动停止");

            // 停止数据采集和报警处理
            await StopDataCollectionAsync(cancellationToken);
            await DisableAlarmHandlingAsync(cancellationToken);

            // 取消所有后台任务
            _serviceCts.Cancel();

            // 等待任务完成
            await Task.WhenAll(
                WaitForTaskCompletion(_messageProcessingTask, "消息处理"),
                WaitForTaskCompletion(_healthMonitoringTask, "健康监控"),
                WaitForTaskCompletion(_dataCollectionTask, "数据采集")
            );

            // 断开设备连接
            await DisconnectInternalAsync(reason, cancellationToken);

            // 更新设备状态
            if (_equipment != null)
            {
                _equipment.UpdateState(EquipmentState.DOWN, reason, "System");
            }

            await ChangeStatusAsync(DeviceServiceStatus.Stopped, "设备服务停止完成");

            _logger.LogInformation("设备服务已停止 [设备ID: {EquipmentId}]", _equipmentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "停止设备服务异常 [设备: {EquipmentId}]: {ErrorMessage}", _equipmentId, ex.Message);
            await ChangeStatusAsync(DeviceServiceStatus.Faulted, $"停止异常: {ex.Message}");
        }
        finally
        {
            _statusSemaphore.Release();
        }
    }

    /// <summary>
    /// 重启设备服务
    /// </summary>
    public async Task RestartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("重启设备服务 [设备ID: {EquipmentId}]", _equipmentId);

        if (_equipment == null)
        {
            throw new InvalidOperationException("设备聚合根未初始化，无法重启服务");
        }

        // 先停止服务
        await StopAsync("重启操作", cancellationToken);

        // 等待一段时间
        await Task.Delay(1000, cancellationToken);

        // 重新启动
        await StartAsync(_equipment, cancellationToken);

        _logger.LogInformation("设备服务重启完成 [设备ID: {EquipmentId}]", _equipmentId);
    }

    #endregion

    #region 连接管理

    /// <summary>
    /// 连接设备
    /// </summary>
    public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        return await ConnectInternalAsync(cancellationToken);
    }

    /// <summary>
    /// 断开设备连接
    /// </summary>
    public async Task DisconnectAsync(string? reason = null, CancellationToken cancellationToken = default)
    {
        await DisconnectInternalAsync(reason, cancellationToken);
    }

    /// <summary>
    /// 重连设备
    /// </summary>
    public async Task<bool> ReconnectAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("重连设备 [设备ID: {EquipmentId}]", _equipmentId);

        await DisconnectInternalAsync("重连操作", cancellationToken);
        await Task.Delay(2000, cancellationToken); // 等待连接清理

        return await ConnectInternalAsync(cancellationToken);
    }

    /// <summary>
    /// 内部连接实现
    /// </summary>
    private async Task<bool> ConnectInternalAsync(CancellationToken cancellationToken)
    {
        await _operationSemaphore.WaitAsync(cancellationToken);
        try
        {
            _logger.LogInformation("连接设备 [设备ID: {EquipmentId}]", _equipmentId);

            var connected = await _hsmsClient.ConnectAsync(cancellationToken);

            if (connected && _equipment != null)
            {
                // 同步设备聚合根连接状态
                var sessionId = _hsmsClient.ConnectionState.SessionId ?? "Unknown";
                _equipment.Connect(sessionId, "SecsDeviceService");

                // 同步设备状态
                await SynchronizeStateInternalAsync(cancellationToken);
            }

            _logger.LogInformation("设备连接{Result} [设备ID: {EquipmentId}]",
                connected ? "成功" : "失败", _equipmentId);

            return connected;
        }
        finally
        {
            _operationSemaphore.Release();
        }
    }

    /// <summary>
    /// 内部断开连接实现
    /// </summary>
    private async Task DisconnectInternalAsync(string? reason, CancellationToken cancellationToken)
    {
        await _operationSemaphore.WaitAsync(cancellationToken);
        try
        {
            _logger.LogInformation("断开设备连接 [设备ID: {EquipmentId}, 原因: {Reason}]",
                _equipmentId, reason ?? "未指定");

            await _hsmsClient.DisconnectAsync(reason, cancellationToken);

            if (_equipment != null)
            {
                // 同步设备聚合根连接状态
                _equipment.Disconnect(reason, "SecsDeviceService");
            }

            _logger.LogInformation("设备连接已断开 [设备ID: {EquipmentId}]", _equipmentId);
        }
        finally
        {
            _operationSemaphore.Release();
        }
    }

    #endregion

    #region 状态管理

    /// <summary>
    /// 同步设备状态
    /// </summary>
    public async Task SynchronizeStateAsync(CancellationToken cancellationToken = default)
    {
        await SynchronizeStateInternalAsync(cancellationToken);
    }

    /// <summary>
    /// 更新设备状态
    /// </summary>
    public async Task<bool> UpdateEquipmentStateAsync(EquipmentState newState, string? reason = null,
        string? operatorId = null, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (_equipment == null)
        {
            _logger.LogWarning("设备聚合根未初始化，无法更新状态 [设备: {EquipmentId}]", _equipmentId);
            return false;
        }

        try
        {
            _logger.LogInformation("更新设备状态 [设备: {EquipmentId}, 新状态: {NewState}, 原因: {Reason}]",
                _equipmentId, newState, reason ?? "未指定");

            // 更新聚合根状态
            _equipment.UpdateState(newState, reason, operatorId);

            // 向设备发送状态变更命令（如果支持远程控制）
            if (_equipment.Configuration.EnableRemoteControl && _hsmsClient.IsConnected)
            {
                await SendStateChangeCommandAsync(newState, cancellationToken);
            }

            _logger.LogInformation("设备状态更新成功 [设备: {EquipmentId}, 状态: {State}]",
                _equipmentId, newState);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新设备状态失败 [设备: {EquipmentId}]: {ErrorMessage}", _equipmentId, ex.Message);
            return false;
        }
    }

    /// <summary>
    /// 获取设备状态信息
    /// </summary>
    public async Task<EquipmentStatus> GetDeviceStateInfoAsync(CancellationToken cancellationToken = default)
    {
        if (_equipment == null)
        {
            throw new InvalidOperationException("设备聚合根未初始化");
        }

        // 可选择性地从设备读取最新状态
        if (_hsmsClient.IsConnected)
        {
            try
            {
                await SynchronizeStateInternalAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "同步设备状态失败，使用缓存状态 [设备: {EquipmentId}]", _equipmentId);
            }
        }

        // 使用 EquipmentStatus.Create 而不是 DeviceStateInfo.Create
        return EquipmentStatus.Create(
            _equipment.Id,
            _equipment.Name,
            _equipment.State,
            _equipment.ConnectionState,
            _equipment.HealthStatus);
    }


    /// <summary>
    /// 内部状态同步实现
    /// </summary>
    private async Task SynchronizeStateInternalAsync(CancellationToken cancellationToken)
    {
        if (!_hsmsClient.IsConnected || _equipment == null)
        {
            return;
        }

        try
        {
            _logger.LogDebug("同步设备状态 [设备ID: {EquipmentId}]", _equipmentId);

            // 从设备读取当前状态
            var currentState = await _hsmsClient.GetEquipmentStateAsync(cancellationToken);

            if (currentState.HasValue && currentState.Value != _equipment.State)
            {
                _logger.LogInformation("检测到设备状态变化 [设备: {EquipmentId}, 旧状态: {OldState}, 新状态: {NewState}]",
                    _equipmentId, _equipment.State, currentState.Value);

                // 更新聚合根状态
                _equipment.UpdateState(currentState.Value, "设备状态同步", "System");
            }

            // 更新心跳
            _equipment.UpdateHeartbeat();

            _logger.LogDebug("设备状态同步完成 [设备: {EquipmentId}, 状态: {State}]",
                _equipmentId, _equipment.State);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "同步设备状态异常 [设备: {EquipmentId}]: {ErrorMessage}", _equipmentId, ex.Message);
            throw;
        }
    }

    #endregion

    #region 数据采集

    /// <summary>
    /// 启动数据采集
    /// </summary>
    public async Task<bool> StartDataCollectionAsync(IEnumerable<uint> dataVariables, IEnumerable<uint> events,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (!_hsmsClient.IsConnected)
        {
            _logger.LogWarning("设备未连接，无法启动数据采集 [设备: {EquipmentId}]", _equipmentId);
            return false;
        }

        try
        {
            _logger.LogInformation("启动数据采集 [设备: {EquipmentId}, DV数量: {DVCount}, EC数量: {ECCount}]",
                _equipmentId, dataVariables.Count(), events.Count());

            // 设置数据变量采集
            foreach (var dv in dataVariables)
            {
                _activeDataVariables[dv] = DateTime.UtcNow;
            }

            // 设置事件采集
            foreach (var ec in events)
            {
                _activeEvents[ec] = DateTime.UtcNow;
            }

            // 发送数据采集启用命令到设备
            var success = await SendDataCollectionEnableCommandAsync(dataVariables, events, cancellationToken);

            if (success)
            {
                _dataCollectionEnabled = true;

                // 启动数据采集任务
                if (_dataCollectionTask == null || _dataCollectionTask.IsCompleted)
                {
                    _dataCollectionTask = CollectDataAsync(_serviceCts.Token);
                }

                _logger.LogInformation("数据采集启动成功 [设备: {EquipmentId}]", _equipmentId);
            }
            else
            {
                _logger.LogError("数据采集启动失败 [设备: {EquipmentId}]", _equipmentId);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "启动数据采集异常 [设备: {EquipmentId}]: {ErrorMessage}", _equipmentId, ex.Message);
            return false;
        }
    }

    /// <summary>
    /// 停止数据采集
    /// </summary>
    public async Task StopDataCollectionAsync(CancellationToken cancellationToken = default)
    {
        if (!_dataCollectionEnabled)
        {
            return;
        }

        try
        {
            _logger.LogInformation("停止数据采集 [设备: {EquipmentId}]", _equipmentId);

            _dataCollectionEnabled = false;

            // 发送数据采集停用命令到设备
            if (_hsmsClient.IsConnected)
            {
                await SendDataCollectionDisableCommandAsync(cancellationToken);
            }

            // 清除活动的数据变量和事件
            _activeDataVariables.Clear();
            _activeEvents.Clear();

            _logger.LogInformation("数据采集已停止 [设备: {EquipmentId}]", _equipmentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "停止数据采集异常 [设备: {EquipmentId}]: {ErrorMessage}", _equipmentId, ex.Message);
        }
    }

    /// <summary>
    /// 请求设备数据
    /// </summary>
    public async Task<IDictionary<uint, object>> RequestDataVariablesAsync(IEnumerable<uint> dataVariables,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (!_hsmsClient.IsConnected)
        {
            throw new InvalidOperationException($"设备 {_equipmentId} 未连接");
        }

        try
        {
            _logger.LogDebug("请求设备数据 [设备: {EquipmentId}, DV: {DataVariables}]",
                _equipmentId, string.Join(",", dataVariables));

            // 构造S2F13请求消息
            var dvList = dataVariables.Select(dv => Item.U4((uint)dv)).ToArray();
            var requestMessage = new SecsMessage(2, 13, replyExpected: true);
            requestMessage.Name = "数据变量请求"; // 设置消息名称（可选，用于调试）
            requestMessage.SecsItem = Item.L(dvList); // 设置消息数据

            // 发送请求并等待响应
            var response = await _hsmsClient.SendAsync(requestMessage, cancellationToken);

            if (response != null)
            {
                // 解析响应数据
                return ParseDataVariablesResponse(response, dataVariables);
            }

            return new Dictionary<uint, object>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "请求设备数据失败 [设备: {EquipmentId}]: {ErrorMessage}", _equipmentId, ex.Message);
            throw;
        }
    }

    #endregion

    #region 报警管理

    /// <summary>
    /// 启用报警处理
    /// </summary>
    public async Task<bool> EnableAlarmHandlingAsync(IEnumerable<uint> alarmIds, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (!_hsmsClient.IsConnected)
        {
            _logger.LogWarning("设备未连接，无法启用报警处理 [设备: {EquipmentId}]", _equipmentId);
            return false;
        }

        try
        {
            _logger.LogInformation("启用报警处理 [设备: {EquipmentId}, 报警数量: {AlarmCount}]",
                _equipmentId, alarmIds.Count());

            // 设置活动报警
            foreach (var alarmId in alarmIds)
            {
                _activeAlarms[alarmId] = DateTime.UtcNow;
            }

            // 发送报警启用命令到设备
            var success = await SendAlarmEnableCommandAsync(alarmIds, cancellationToken);

            if (success)
            {
                _alarmHandlingEnabled = true;
                _logger.LogInformation("报警处理启用成功 [设备: {EquipmentId}]", _equipmentId);
            }
            else
            {
                _logger.LogError("报警处理启用失败 [设备: {EquipmentId}]", _equipmentId);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "启用报警处理异常 [设备: {EquipmentId}]: {ErrorMessage}", _equipmentId, ex.Message);
            return false;
        }
    }

    /// <summary>
    /// 禁用报警处理
    /// </summary>
    public async Task DisableAlarmHandlingAsync(CancellationToken cancellationToken = default)
    {
        if (!_alarmHandlingEnabled)
        {
            return;
        }

        try
        {
            _logger.LogInformation("禁用报警处理 [设备: {EquipmentId}]", _equipmentId);

            _alarmHandlingEnabled = false;

            // 发送报警禁用命令到设备
            if (_hsmsClient.IsConnected)
            {
                await SendAlarmDisableCommandAsync(cancellationToken);
            }

            // 清除活动报警
            _activeAlarms.Clear();

            _logger.LogInformation("报警处理已禁用 [设备: {EquipmentId}]", _equipmentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "禁用报警处理异常 [设备: {EquipmentId}]: {ErrorMessage}", _equipmentId, ex.Message);
        }
    }

    /// <summary>
    /// 确认报警
    /// </summary>
    public async Task<bool> AcknowledgeAlarmAsync(uint alarmId, string operatorId, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (!_hsmsClient.IsConnected)
        {
            _logger.LogWarning("设备未连接，无法确认报警 [设备: {EquipmentId}, 报警: {AlarmId}]", _equipmentId, alarmId);
            return false;
        }

        try
        {
            _logger.LogInformation("确认报警 [设备: {EquipmentId}, 报警: {AlarmId}, 操作员: {OperatorId}]",
                _equipmentId, alarmId, operatorId);

            // 发送报警确认命令到设备
            var success = await SendAlarmAcknowledgeCommandAsync(alarmId, operatorId, cancellationToken);

            if (success && _equipment != null)
            {
                // 确认设备聚合根中的报警
                _equipment.AcknowledgeAlarm((ushort)alarmId, operatorId);
            }

            _logger.LogInformation("报警确认{Result} [设备: {EquipmentId}, 报警: {AlarmId}]",
                success ? "成功" : "失败", _equipmentId, alarmId);

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "确认报警异常 [设备: {EquipmentId}, 报警: {AlarmId}]: {ErrorMessage}",
                _equipmentId, alarmId, ex.Message);
            return false;
        }
    }

    /// <summary>
    /// 获取活动报警列表
    /// </summary>
    public async Task<IReadOnlyList<AlarmEvent>> GetActiveAlarmsAsync(CancellationToken cancellationToken = default)
    {
        if (_equipment == null)
        {
            return Array.Empty<AlarmEvent>();
        }

        // 如果连接正常，先同步最新报警状态
        if (_hsmsClient.IsConnected)
        {
            try
            {
                await SynchronizeAlarmsAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "同步报警状态失败，使用缓存数据 [设备: {EquipmentId}]", _equipmentId);
            }
        }

        return _equipment.ActiveAlarms;
    }

    #endregion

    #region 远程控制

    /// <summary>
    /// 发送远程控制命令
    /// </summary>
    public async Task<RemoteCommandResult> SendRemoteCommandAsync(string command,
        IDictionary<string, object>? parameters = null, string? operatorId = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (_equipment == null)
        {
            throw new InvalidOperationException("设备聚合根未初始化");
        }

        if (!_equipment.Configuration.EnableRemoteControl)
        {
            throw new InvalidOperationException($"设备 {_equipmentId} 未启用远程控制功能");
        }

        if (!_hsmsClient.IsConnected)
        {
            throw new InvalidOperationException($"设备 {_equipmentId} 未连接");
        }

        var stopwatch = Stopwatch.StartNew();
        var commandId = Guid.NewGuid();

        try
        {
            _logger.LogInformation("发送远程控制命令 [设备: {EquipmentId}, 命令: {Command}, 操作员: {OperatorId}]",
                _equipmentId, command, operatorId ?? "System");

            // 在设备聚合根中记录命令
            var aggregateCommandId = _equipment.ExecuteRemoteCommand(command, parameters, operatorId);

            // 根据命令类型构造SECS消息
            var secsMessage = BuildRemoteCommandMessage(command, parameters);

            // 发送命令到设备
            var response = await _hsmsClient.SendAsync(secsMessage, cancellationToken);

            stopwatch.Stop();

            if (response != null)
            {
                // 解析命令执行结果
                var success = ParseCommandResponse(response);
                var resultMessage = success ? "命令执行成功" : "命令执行失败";

                // 更新聚合根中的命令状态
                _equipment.UpdateCommandStatus(aggregateCommandId,
                    success ? CommandStatus.Completed : CommandStatus.Failed, resultMessage);

                _logger.LogInformation("远程控制命令{Result} [设备: {EquipmentId}, 命令: {Command}, 耗时: {Duration}ms]",
                    success ? "成功" : "失败", _equipmentId, command, stopwatch.ElapsedMilliseconds);

                return success
                    ? RemoteCommandResult.Success(commandId, command, stopwatch.Elapsed, resultMessage)
                    : RemoteCommandResult.Failure(commandId, command, resultMessage, stopwatch.Elapsed);
            }
            else
            {
                // 更新聚合根中的命令状态
                _equipment.UpdateCommandStatus(aggregateCommandId, CommandStatus.Failed, "未收到设备响应");

                return RemoteCommandResult.Failure(commandId, command, "未收到设备响应", stopwatch.Elapsed);
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(ex, "发送远程控制命令异常 [设备: {EquipmentId}, 命令: {Command}]: {ErrorMessage}",
                _equipmentId, command, ex.Message);

            return RemoteCommandResult.Failure(commandId, command, ex.Message, stopwatch.Elapsed);
        }
    }

    /// <summary>
    /// 设备启动命令
    /// </summary>
    public async Task<bool> StartEquipmentAsync(string? operatorId = null, CancellationToken cancellationToken = default)
    {
        var result = await SendRemoteCommandAsync("START", null, operatorId, cancellationToken);
        return result.IsSuccessful;
    }

    /// <summary>
    /// 设备停止命令
    /// </summary>
    public async Task<bool> StopEquipmentAsync(string? operatorId = null, CancellationToken cancellationToken = default)
    {
        var result = await SendRemoteCommandAsync("STOP", null, operatorId, cancellationToken);
        return result.IsSuccessful;
    }

    /// <summary>
    /// 设备暂停命令
    /// </summary>
    public async Task<bool> PauseEquipmentAsync(string? operatorId = null, CancellationToken cancellationToken = default)
    {
        var result = await SendRemoteCommandAsync("PAUSE", null, operatorId, cancellationToken);
        return result.IsSuccessful;
    }

    /// <summary>
    /// 设备恢复命令
    /// </summary>
    public async Task<bool> ResumeEquipmentAsync(string? operatorId = null, CancellationToken cancellationToken = default)
    {
        var result = await SendRemoteCommandAsync("RESUME", null, operatorId, cancellationToken);
        return result.IsSuccessful;
    }

    /// <summary>
    /// 设备复位命令
    /// </summary>
    public async Task<bool> ResetEquipmentAsync(string? operatorId = null, CancellationToken cancellationToken = default)
    {
        var result = await SendRemoteCommandAsync("RESET", null, operatorId, cancellationToken);
        return result.IsSuccessful;
    }

    #endregion

    #region 健康检查和诊断

    /// <summary>
    /// 执行设备健康检查
    /// </summary>
    public async Task<DeviceHealthCheckResult> PerformHealthCheckAsync(CancellationToken cancellationToken = default)
    {
        var checkItems = new List<HealthCheckItem>();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogDebug("执行设备健康检查 [设备: {EquipmentId}]", _equipmentId);

            // 检查服务状态
            checkItems.Add(HealthCheckItem.Create(
                "服务状态", "Service",
                _status == DeviceServiceStatus.Started ? HealthStatus.Healthy : HealthStatus.Unhealthy,
                $"当前状态: {_status}"));

            // 检查连接状态
            var connectionStatus = _hsmsClient.IsConnected ? HealthStatus.Healthy : HealthStatus.Unhealthy;
            checkItems.Add(HealthCheckItem.Create(
                "连接状态", "Connection", connectionStatus,
                $"连接状态: {(_hsmsClient.IsConnected ? "已连接" : "已断开")}"));

            // 检查设备状态
            if (_equipment != null)
            {
                var equipmentHealthStatus = _equipment.State.IsAvailable() ? HealthStatus.Healthy : HealthStatus.Unhealthy;
                checkItems.Add(HealthCheckItem.Create(
                    "设备状态", "Equipment", equipmentHealthStatus,
                    $"设备状态: {_equipment.State.GetDisplayName()}"));

                // 检查报警状态
                var activeAlarms = _equipment.ActiveAlarms.Count;
                var criticalAlarms = _equipment.ActiveAlarms.Count(a => a.Severity >= AlarmSeverity.MAJOR);
                var alarmStatus = criticalAlarms > 0 ? HealthStatus.Unhealthy :
                                  activeAlarms > 0 ? HealthStatus.Degraded : HealthStatus.Healthy;
                checkItems.Add(HealthCheckItem.Create(
                    "报警状态", "Alarm", alarmStatus,
                    $"活动报警: {activeAlarms}, 严重报警: {criticalAlarms}"));
            }

            // 检查通信质量
            if (_hsmsClient.IsConnected)
            {
                try
                {
                    var testResult = await _hsmsClient.TestConnectionAsync(cancellationToken);
                    var commStatus = testResult.IsSuccessful ? HealthStatus.Healthy : HealthStatus.Degraded;
                    checkItems.Add(HealthCheckItem.Create(
                        "通信测试", "Communication", commStatus,
                        $"响应时间: {testResult.ResponseTime.TotalMilliseconds:F1}ms"));
                }
                catch (Exception ex)
                {
                    checkItems.Add(HealthCheckItem.Create(
                        "通信测试", "Communication", HealthStatus.Unhealthy,
                        $"测试失败: {ex.Message}"));
                }
            }

            stopwatch.Stop();

            // 确定整体健康状态
            var overallHealth = DetermineOverallHealth(checkItems);

            var result = new DeviceHealthCheckResult
            {
                EquipmentId = _equipmentId,
                OverallHealth = overallHealth,
                CheckItems = checkItems.AsReadOnly(),
                CheckTime = DateTime.UtcNow,
                CheckDuration = stopwatch.Elapsed
            };

            _logger.LogDebug("设备健康检查完成 [设备: {EquipmentId}, 状态: {Health}, 耗时: {Duration}ms]",
                _equipmentId, overallHealth, stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "设备健康检查异常 [设备: {EquipmentId}]: {ErrorMessage}", _equipmentId, ex.Message);

            checkItems.Add(HealthCheckItem.Create(
                "健康检查", "System", HealthStatus.Unhealthy, $"检查异常: {ex.Message}"));

            return new DeviceHealthCheckResult
            {
                EquipmentId = _equipmentId,
                OverallHealth = HealthStatus.Unhealthy,
                CheckItems = checkItems.AsReadOnly(),
                CheckTime = DateTime.UtcNow,
                CheckDuration = stopwatch.Elapsed
            };
        }
    }

    /// <summary>
    /// 获取设备诊断信息
    /// </summary>
    public Task<DeviceDiagnosticInfo> GetDiagnosticInfoAsync(CancellationToken cancellationToken = default)
    {
        // 修复类型推断问题：使用 TimeSpan? (可空类型)
        var uptime = _status == DeviceServiceStatus.Started
            ? (TimeSpan?)(DateTime.UtcNow - _serviceStartedAt)
            : null;

        var diagnosticInfo = new DeviceDiagnosticInfo
        {
            EquipmentId = _equipmentId,
            ServiceStatus = _status,
            ConnectionState = _hsmsClient.ConnectionState,
            EquipmentState = _equipment?.State ?? EquipmentState.UNKNOWN,
            HealthStatus = HealthStatus,
            ActiveAlarmCount = _equipment?.ActiveAlarms.Count ?? 0,
            LastDataReceived = _equipment?.LastDataUpdate,
            LastHeartbeat = _hsmsClient.LastHeartbeat,
            Uptime = uptime,
            Metrics = new Dictionary<string, object>
            {
                ["DataMessagesReceived"] = _dataMessagesReceived,
                ["AlarmMessagesReceived"] = _alarmMessagesReceived,
                ["ControlMessagesReceived"] = _controlMessagesReceived,
                ["DataCollectionEnabled"] = _dataCollectionEnabled,
                ["AlarmHandlingEnabled"] = _alarmHandlingEnabled,
                ["ActiveDataVariables"] = _activeDataVariables.Count,
                ["ActiveEvents"] = _activeEvents.Count,
                ["ActiveAlarms"] = _activeAlarms.Count
            },
            DiagnosticTime = DateTime.UtcNow
        };

        // 由于没有异步操作，直接返回 Task.FromResult
        return Task.FromResult(diagnosticInfo);
    }

    /// <summary>
    /// 获取通信统计信息
    /// </summary>
    public CommunicationStatistics GetCommunicationStatistics()
    {
        var connectionStats = _hsmsClient.GetConnectionStatistics();
        return CommunicationStatistics.Create(_equipmentId, connectionStats);
    }

    #endregion

    #region 配置管理

    /// <summary>
    /// 更新设备配置
    /// </summary>
    public async Task<bool> UpdateConfigurationAsync(EquipmentConfiguration newConfiguration, string? updatedBy = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (_equipment == null)
        {
            throw new InvalidOperationException("设备聚合根未初始化");
        }

        try
        {
            _logger.LogInformation("更新设备配置 [设备: {EquipmentId}, 更新者: {UpdatedBy}]",
                _equipmentId, updatedBy ?? "System");

            var oldConfiguration = _equipment.Configuration;

            // 更新聚合根配置
            _equipment.UpdateConfiguration(newConfiguration, updatedBy);

            // 只检查网络端点变化，移除 DeviceId 检查
            if (!oldConfiguration.Endpoint.Equals(newConfiguration.Endpoint))
            {
                _logger.LogInformation("网络配置发生变化，重新建立连接 [设备: {EquipmentId}]", _equipmentId);

                // 断开当前连接
                await DisconnectInternalAsync("配置更新", cancellationToken);

                // 等待一段时间后重新连接
                await Task.Delay(2000, cancellationToken);

                // 重新连接
                await ConnectInternalAsync(cancellationToken);
            }

            _logger.LogInformation("设备配置更新完成 [设备: {EquipmentId}]", _equipmentId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新设备配置失败 [设备: {EquipmentId}]: {ErrorMessage}", _equipmentId, ex.Message);
            return false;
        }
    }

    /// <summary>
    /// 获取当前配置
    /// </summary>
    public EquipmentConfiguration GetCurrentConfiguration()
    {
        if (_equipment == null)
        {
            throw new InvalidOperationException("设备聚合根未初始化");
        }

        return _equipment.Configuration;
    }

    #endregion

    #region 事件处理

    /// <summary>
    /// 连接状态变化事件处理
    /// </summary>
    private void OnConnectionStateChanged(object? sender, ConnectionStateChangedEventArgs e)
    {
        if (_disposed || _equipment == null)
            return;

        try
        {
            _logger.LogDebug("处理连接状态变化 [设备: {EquipmentId}, 状态: {OldState} → {NewState}]",
                _equipmentId,
                e.OldState.IsConnected ? "已连接" : "已断开",
                e.NewState.IsConnected ? "已连接" : "已断开");

            // 同步操作
            if (e.NewState.IsConnected && !e.OldState.IsConnected)
            {
                var sessionId = e.NewState.SessionId ?? e.SessionId ?? "Unknown";
                _equipment.Connect(sessionId, "SecsDeviceService");

                // 异步操作使用 fire-and-forget 模式
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await SynchronizeStateInternalAsync(CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "连接建立后同步状态失败 [设备: {EquipmentId}]", _equipmentId);
                    }
                });
            }
            else if (!e.NewState.IsConnected && e.OldState.IsConnected)
            {
                _equipment.Disconnect(e.Reason, "SecsDeviceService");

                // 清理操作也使用 fire-and-forget
                _ = Task.Run(async () =>
                {
                    try
                    {
                        // 停止数据采集
                        _dataCollectionEnabled = false;
                        _activeDataVariables.Clear();
                        _activeEvents.Clear();
                        _activeAlarms.Clear();

                        await Task.CompletedTask;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "连接断开后清理失败 [设备: {EquipmentId}]", _equipmentId);
                    }
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理连接状态变化异常 [设备: {EquipmentId}]: {ErrorMessage}", _equipmentId, ex.Message);
        }
    }

    /// <summary>
    /// 消息接收事件处理
    /// </summary>
    private async void OnMessageReceived(object? sender, SecsMessageReceivedEventArgs e)
    {
        if (_disposed)
            return;

        try
        {
            await ProcessIncomingMessageAsync(e.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理接收消息异常 [设备: {EquipmentId}, 消息: {MessageType}]: {ErrorMessage}",
                _equipmentId, e.MessageType, ex.Message);
        }
    }

    /// <summary>
    /// 消息超时事件处理
    /// </summary>
    private async void OnMessageTimeout(object? sender, MessageTimeoutEventArgs e)
    {
        if (_disposed)
            return;

        try
        {
            _logger.LogWarning("消息超时 [设备: {EquipmentId}, 消息: {MessageType}, 超时: {Timeout}ms]",
                _equipmentId, e.MessageType, e.Timeout.TotalMilliseconds);

            // 发布消息超时领域事件
            await _mediator.Publish(new Core.Events.Message.MessageTimeoutEvent(
                _equipmentId, e.Message.S, e.Message.F, 0, e.SentAt, e.TimeoutAt));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理消息超时异常 [设备: {EquipmentId}]: {ErrorMessage}", _equipmentId, ex.Message);
        }
    }

    #endregion

    #region 私有辅助方法

    /// <summary>
    /// 更改服务状态
    /// </summary>
    private async Task ChangeStatusAsync(DeviceServiceStatus newStatus, string? reason)
    {
        var previousStatus = _status;
        _status = newStatus;

        _logger.LogDebug("设备服务状态变化 [设备: {EquipmentId}, 状态: {PreviousStatus} → {NewStatus}]",
            _equipmentId, previousStatus, newStatus);

        // 触发状态变化事件
        StatusChanged?.Invoke(this, new DeviceServiceStatusChangedEventArgs(_equipmentId, previousStatus, newStatus, reason));

        await Task.CompletedTask;
    }

    /// <summary>
    /// 等待任务完成
    /// </summary>
    private async Task WaitForTaskCompletion(Task? task, string taskName)
    {
        if (task == null)
            return;

        try
        {
            await task;
        }
        catch (OperationCanceledException)
        {
            // 正常取消，不记录错误
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "等待任务完成异常 [设备: {EquipmentId}, 任务: {TaskName}]: {ErrorMessage}",
                _equipmentId, taskName, ex.Message);
        }
    }

    /// <summary>
    /// 处理传入消息
    /// </summary>
    private async Task ProcessIncomingMessageAsync(SecsMessage message)
    {
        _logger.LogDebug("处理传入消息 [设备: {EquipmentId}, 消息: S{Stream}F{Function}]",
            _equipmentId, message.S, message.F);

        // 根据消息类型进行分类处理
        switch (message.S)
        {
            case 2: // 设备控制相关
                ProcessEquipmentControlMessage(message); // 改为同步调用 暂时同步调用
                break;
            case 5: // 异常报警相关
                await ProcessAlarmMessageAsync(message);
                break;
            case 6: // 数据采集相关
                await ProcessDataCollectionMessageAsync(message);
                break;
            default:
                _logger.LogDebug("未处理的消息类型 [设备: {EquipmentId}, 消息: S{Stream}F{Function}]",
                    _equipmentId, message.S, message.F);
                break;
        }
    }

    /// <summary>
    /// 处理设备控制消息（同步版本）
    /// </summary>
    private void ProcessEquipmentControlMessage(SecsMessage message)
    {
        Interlocked.Increment(ref _controlMessagesReceived);

        // 根据具体的Function处理不同的控制消息
        // 这里添加具体的控制消息处理逻辑
        _logger.LogDebug("处理设备控制消息 [设备: {EquipmentId}, 消息: S{Stream}F{Function}]",
            _equipmentId, message.S, message.F);
    }


    /// <summary>
    /// 处理报警消息
    /// </summary>
    private async Task ProcessAlarmMessageAsync(SecsMessage message)
    {
        Interlocked.Increment(ref _alarmMessagesReceived);

        if (_equipment == null || !_alarmHandlingEnabled)
            return;

        try
        {
            // 解析报警消息
            var alarmData = ParseAlarmMessage(message);
            if (alarmData != null)
            {
                // 根据报警状态更新聚合根
                if (alarmData.IsSet)
                {
                    _equipment.AddAlarm(alarmData.AlarmId, alarmData.AlarmText, alarmData.Severity);
                }
                else
                {
                    _equipment.ClearAlarm(alarmData.AlarmId, "设备自动清除");
                }

                // 创建报警事件对象 - 修复：使用构造函数而不是 Create 静态方法
                var alarmEvent = new AlarmEvent(
                    _equipmentId.ToString(),
                    alarmData.AlarmId,
                    null, // alarmCode - 可选
                    alarmData.AlarmText,
                    alarmData.Severity);

                // 触发报警事件 - 修复：使用字符串类型而不是枚举
                var eventType = alarmData.IsSet ? "Triggered" : "Cleared";
                AlarmEvent?.Invoke(this, new DeviceAlarmEventArgs(_equipmentId, alarmEvent, eventType));

                // 发布领域事件到中介者
                if (alarmData.IsSet)
                {
                    await _mediator.Publish(new AlarmTriggeredEvent(_equipmentId, alarmEvent, DateTime.UtcNow));
                }
                else
                {
                    await _mediator.Publish(new AlarmClearedEvent(_equipmentId, alarmEvent, "设备自动清除", DateTime.UtcNow));
                }

                _logger.LogInformation("处理报警消息 [设备: {EquipmentId}, 报警: {AlarmId}, 状态: {Status}]",
                    _equipmentId, alarmData.AlarmId, alarmData.IsSet ? "触发" : "清除");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理报警消息异常 [设备: {EquipmentId}]: {ErrorMessage}", _equipmentId, ex.Message);
        }
    }

    /// <summary>
    /// 处理数据采集消息（修复版本）
    /// </summary>
    private Task ProcessDataCollectionMessageAsync(SecsMessage message)
    {
        Interlocked.Increment(ref _dataMessagesReceived);

        if (_equipment == null || !_dataCollectionEnabled)
            return Task.CompletedTask;

        try
        {
            // 解析数据采集消息
            var traceData = ParseDataMessage(message);

            if (traceData is not null)
            {
                // 添加到设备聚合根
                _equipment.AddTraceData(traceData.ReportId, traceData.DataValues, traceData.Timestamp);

                // 修复：正确构造 DeviceDataReceivedEventArgs
                // 需要传递所有必需的参数，包括 dataType
                DataReceived?.Invoke(this, new DeviceDataReceivedEventArgs(
                    _equipmentId,
                    traceData.DataValues.AsReadOnly(), // 转换为只读字典
                    "TraceData",  // dataType 参数
                    "SECS/GEM",   // source 参数
                    traceData.LotId,     // lotId 参数
                    traceData.CarrierId  // carrierId 参数
                ));

                _logger.LogDebug("处理数据采集消息 [设备: {EquipmentId}, 报告: {ReportId}, 数据项: {ItemCount}]",
                    _equipmentId, traceData.ReportId, traceData.ItemCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理数据采集消息异常 [设备: {EquipmentId}]: {ErrorMessage}", _equipmentId, ex.Message);
        }

        // 返回已完成的任务，因为这个方法中没有实际的异步操作
        return Task.CompletedTask;
    }


    /// <summary>
    /// 处理消息任务
    /// </summary>
    private async Task ProcessMessagesAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("启动消息处理任务 [设备: {EquipmentId}]", _equipmentId);

        try
        {
            await foreach (var message in _hsmsClient.GetPrimaryMessageAsync(cancellationToken))
            {
                await ProcessIncomingMessageAsync(message);
            }
        }
        catch (OperationCanceledException)
        {
            // 正常取消
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "消息处理任务异常 [设备: {EquipmentId}]: {ErrorMessage}", _equipmentId, ex.Message);
        }

        _logger.LogDebug("消息处理任务已停止 [设备: {EquipmentId}]", _equipmentId);
    }

    /// <summary>
    /// 健康监控任务
    /// </summary>
    private async Task MonitorHealthAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("启动健康监控任务 [设备: {EquipmentId}]", _equipmentId);

        const int checkIntervalMs = 30000; // 30秒检查一次

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await PerformHealthCheckAsync(cancellationToken);
                    await Task.Delay(checkIntervalMs, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "健康检查异常 [设备: {EquipmentId}]: {ErrorMessage}", _equipmentId, ex.Message);
                    await Task.Delay(checkIntervalMs, cancellationToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // 正常取消
        }

        _logger.LogDebug("健康监控任务已停止 [设备: {EquipmentId}]", _equipmentId);
    }

    /// <summary>
    /// 数据采集任务
    /// </summary>
    private async Task CollectDataAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("启动数据采集任务 [设备: {EquipmentId}]", _equipmentId);

        const int collectIntervalMs = 5000; // 5秒采集一次

        try
        {
            while (!cancellationToken.IsCancellationRequested && _dataCollectionEnabled)
            {
                try
                {
                    if (_hsmsClient.IsConnected && _activeDataVariables.Any())
                    {
                        // 主动请求数据变量
                        var dataVariables = _activeDataVariables.Keys.ToList();
                        var data = await RequestDataVariablesAsync(dataVariables, cancellationToken);

                        if (data.Any())
                        {
                            _logger.LogDebug("采集到数据 [设备: {EquipmentId}, 数据项: {ItemCount}]",
                                _equipmentId, data.Count);
                        }
                    }

                    await Task.Delay(collectIntervalMs, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "数据采集异常 [设备: {EquipmentId}]: {ErrorMessage}", _equipmentId, ex.Message);
                    await Task.Delay(collectIntervalMs, cancellationToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // 正常取消
        }

        _logger.LogDebug("数据采集任务已停止 [设备: {EquipmentId}]", _equipmentId);
    }

    /// <summary>
    /// 发送状态变更命令
    /// </summary>
    private async Task SendStateChangeCommandAsync(EquipmentState newState, CancellationToken cancellationToken)
    {
        try
        {
            // 构造状态变更命令消息 (S2F41)
            var stateValue = (byte)newState;
            var message = new SecsMessage(2, 41, true);
            message.Name = "状态变更命令";
            message.SecsItem = Item.L(Item.U1(stateValue));


            await _hsmsClient.SendAsync(message, cancellationToken);

            _logger.LogDebug("发送状态变更命令 [设备: {EquipmentId}, 状态: {State}]", _equipmentId, newState);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送状态变更命令失败 [设备: {EquipmentId}, 状态: {State}]: {ErrorMessage}",
                _equipmentId, newState, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// 发送数据采集启用命令
    /// </summary>
    private async Task<bool> SendDataCollectionEnableCommandAsync(IEnumerable<uint> dataVariables,
        IEnumerable<uint> events, CancellationToken cancellationToken)
    {
        try
        {
            // 构造数据采集启用命令 (S2F37)
            var dvItems = dataVariables.Select(dv => Item.U4(dv)).ToArray();
            var ecItems = events.Select(ec => Item.U4(ec)).ToArray();

            var message = new SecsMessage(2, 37, replyExpected: true);
            message.Name = "启用数据采集"; // 设置消息名称用于调试
            message.SecsItem = Item.L(
                Item.L(dvItems), // 数据变量列表
                Item.L(ecItems)  // 事件列表
            );


            var response = await _hsmsClient.SendAsync(message, cancellationToken);

            // 检查响应结果
            var success = response != null && ParseCommandResponse(response);

            _logger.LogInformation("数据采集启用命令{Result} [设备: {EquipmentId}]",
                success ? "成功" : "失败", _equipmentId);

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送数据采集启用命令失败 [设备: {EquipmentId}]: {ErrorMessage}",
                _equipmentId, ex.Message);
            return false;
        }
    }

    /// <summary>
    /// 发送数据采集停用命令
    /// </summary>
    private async Task SendDataCollectionDisableCommandAsync(CancellationToken cancellationToken)
    {
        try
        {
            // 构造数据采集停用命令 (S2F39)
            var message = new SecsMessage(2, 39, replyExpected: true);
            message.Name = "停用数据采集"; // 设置消息名称用于调试
            message.SecsItem = Item.L();

            await _hsmsClient.SendAsync(message, cancellationToken);

            _logger.LogDebug("发送数据采集停用命令 [设备: {EquipmentId}]", _equipmentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送数据采集停用命令失败 [设备: {EquipmentId}]: {ErrorMessage}",
                _equipmentId, ex.Message);
        }
    }

    /// <summary>
    /// 发送报警启用命令
    /// </summary>
    private async Task<bool> SendAlarmEnableCommandAsync(IEnumerable<uint> alarmIds, CancellationToken cancellationToken)
    {
        try
        {
            // 构造报警启用命令 (S5F3)
            var alarmItems = alarmIds.Select(id => Item.U4(id)).ToArray();

            var message = new SecsMessage(5, 3, replyExpected: true);
            message.Name = "启用报警";
            message.SecsItem = Item.L(alarmItems);

            var response = await _hsmsClient.SendAsync(message, cancellationToken);
            var success = response != null && ParseCommandResponse(response);

            _logger.LogInformation("报警启用命令{Result} [设备: {EquipmentId}]",
                success ? "成功" : "失败", _equipmentId);

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送报警启用命令失败 [设备: {EquipmentId}]: {ErrorMessage}",
                _equipmentId, ex.Message);
            return false;
        }
    }

    /// <summary>
    /// 发送报警停用命令
    /// </summary>
    private async Task SendAlarmDisableCommandAsync(CancellationToken cancellationToken)
    {
        try
        {
            // 构造报警停用命令 (S5F5)
            var message = new SecsMessage(5, 3, replyExpected: true);
            message.Name = "停用报警";
            message.SecsItem = Item.L();

            await _hsmsClient.SendAsync(message, cancellationToken);

            _logger.LogDebug("发送报警停用命令 [设备: {EquipmentId}]", _equipmentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送报警停用命令失败 [设备: {EquipmentId}]: {ErrorMessage}",
                _equipmentId, ex.Message);
        }
    }

    /// <summary>
    /// 发送报警确认命令
    /// </summary>
    private async Task<bool> SendAlarmAcknowledgeCommandAsync(uint alarmId, string operatorId, CancellationToken cancellationToken)
    {
        try
        {
            // 构造报警确认命令 (S5F1)
            var message = new SecsMessage(5, 1, replyExpected: true);
            message.Name = "确认报警";
            message.SecsItem = Item.L(
                    Item.U4(alarmId),
                    Item.A(operatorId)
                );

            var response = await _hsmsClient.SendAsync(message, cancellationToken);
            var success = response != null && ParseCommandResponse(response);

            _logger.LogInformation("报警确认命令{Result} [设备: {EquipmentId}, 报警: {AlarmId}]",
                success ? "成功" : "失败", _equipmentId, alarmId);

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送报警确认命令失败 [设备: {EquipmentId}, 报警: {AlarmId}]: {ErrorMessage}",
                _equipmentId, alarmId, ex.Message);
            return false;
        }
    }

    /// <summary>
    /// 构造远程控制命令消息
    /// </summary>
    private SecsMessage BuildRemoteCommandMessage(string command, IDictionary<string, object>? parameters)
    {
        // 根据命令类型构造不同的SECS消息
        return command.ToUpperInvariant() switch
        {
            "START" => CreateSecsMessage(2, 41, "启动命令", Item.L(Item.U1(1))),
            "STOP" => CreateSecsMessage(2, 41, "停止命令", Item.L(Item.U1(2))),
            "PAUSE" => CreateSecsMessage(2, 41, "暂停命令", Item.L(Item.U1(3))),
            "RESUME" => CreateSecsMessage(2, 41, "恢复命令", Item.L(Item.U1(4))),
            "RESET" => CreateSecsMessage(2, 41, "复位命令", Item.L(Item.U1(5))),
            _ => CreateSecsMessage(2, 49, "通用命令", Item.L(Item.A(command)))
        };
    }

    /// <summary>
    /// 创建SECS消息的通用辅助方法
    /// </summary>
    /// <param name="s">Stream编号</param>
    /// <param name="f">Function编号</param>
    /// <param name="name">消息名称（可选）</param>
    /// <param name="data">消息数据项（可选）</param>
    /// <param name="replyExpected">是否期望回复</param>
    /// <returns>SECS消息</returns>
    private SecsMessage CreateSecsMessage(byte s, byte f, string? name = null, Item? data = null, bool replyExpected = true)
    {
        var message = new SecsMessage(s, f, replyExpected);

        if (!string.IsNullOrEmpty(name))
        {
            message.Name = name;
        }

        if (data != null)
        {
            message.SecsItem = data;
        }

        return message;
    }

    /// <summary>
    /// 解析命令响应（修复版本）
    /// </summary>
    private bool ParseCommandResponse(SecsMessage response)
    {
        try
        {
            // 简单的响应解析逻辑
            if (response.SecsItem != null)
            {
                // 假设响应格式为 L{U1[HCACK] U1[ErrCode]}
                var items = response.SecsItem.Items;
                if (items != null && items.Length >= 1)
                {
                    var ack = items[0];
                    // HCACK = 0 表示成功
                    // 修复：使用 FirstValue<T>() 方法
                    return ack.FirstValue<byte>() == 0;
                }
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 解析数据变量响应（修复版本）
    /// </summary>
    private IDictionary<uint, object> ParseDataVariablesResponse(SecsMessage response, IEnumerable<uint> requestedVariables)
    {
        var result = new Dictionary<uint, object>();

        try
        {
            if (response.SecsItem?.Items != null)
            {
                var variableList = requestedVariables.ToArray();
                var valueItems = response.SecsItem.Items;

                for (int i = 0; i < Math.Min(variableList.Length, valueItems.Length); i++)
                {
                    var variableId = variableList[i];
                    var value = ExtractItemValue(valueItems[i]);
                    result[variableId] = value;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解析数据变量响应失败 [设备: {EquipmentId}]: {ErrorMessage}", _equipmentId, ex.Message);
        }

        return result;
    }

    /// <summary>
    /// 解析报警消息（修复版本）
    /// </summary>
    private AlarmMessageData? ParseAlarmMessage(SecsMessage message)
    {
        try
        {
            // 根据消息类型解析报警数据
            if (message.S == 5 && (message.F == 1 || message.F == 6))
            {
                var items = message.SecsItem?.Items;
                if (items != null && items.Length >= 3)
                {
                    return new AlarmMessageData
                    {
                        // 修复：使用 FirstValue<T>() 方法
                        AlarmId = (ushort)items[0].FirstValue<uint>(),
                        AlarmText = ExtractStringValue(items[1]) ?? "未知报警",
                        Severity = (AlarmSeverity)items[2].FirstValue<byte>(),
                        IsSet = message.F == 1 // F1=设置, F6=清除
                    };
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解析报警消息失败 [设备: {EquipmentId}]: {ErrorMessage}", _equipmentId, ex.Message);
        }

        return null;
    }


    /// <summary>
    /// 解析数据消息（修复版本）
    /// </summary>
    private TraceData? ParseDataMessage(SecsMessage message)
    {
        try
        {
            // 根据消息类型解析数据
            if (message.S == 6 && (message.F == 1 || message.F == 11))
            {
                var reportId = ExtractReportId(message);
                var dataValues = ExtractDataValues(message);

                if (dataValues.Any())
                {
                    // 使用修复后的 Create 方法
                    return TraceData.Create(
                        _equipmentId.Value, // 使用 EquipmentId.Value 
                        reportId,
                        dataValues);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解析数据消息失败 [设备: {EquipmentId}]: {ErrorMessage}", _equipmentId, ex.Message);
        }

        return null;
    }

    /// <summary>
    /// 同步报警状态
    /// </summary>
    private async Task SynchronizeAlarmsAsync(CancellationToken cancellationToken)
    {
        try
        {
            // 发送S5F7请求获取当前报警列表
            var message = CreateSecsMessage(5, 7, "获取报警列表", Item.L());

            var response = await _hsmsClient.SendAsync(message, cancellationToken);

            if (response?.SecsItem != null && _equipment != null)
            {
                // 解析报警列表并同步到聚合根
                // 这里添加具体的报警列表解析逻辑
                _logger.LogDebug("同步报警状态完成 [设备: {EquipmentId}]", _equipmentId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "同步报警状态失败 [设备: {EquipmentId}]: {ErrorMessage}", _equipmentId, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// 提取Item值（修复可空引用类型版本）
    /// </summary>
    private object ExtractItemValue(Item item)
    {
        try
        {
            // 根据Item格式提取值，处理可空引用类型
            return item.Format switch
            {
                SecsFormat.U1 => item.FirstValue<byte>(),
                SecsFormat.U2 => item.FirstValue<ushort>(),
                SecsFormat.U4 => item.FirstValue<uint>(),
                SecsFormat.I1 => item.FirstValue<sbyte>(),
                SecsFormat.I2 => item.FirstValue<short>(),
                SecsFormat.I4 => item.FirstValue<int>(),
                SecsFormat.F4 => item.FirstValue<float>(),
                SecsFormat.F8 => item.FirstValue<double>(),

                // 修复：对于 ASCII 字符串，使用特殊处理
                SecsFormat.ASCII => ExtractStringValue(item),

                // 修复：对于 Binary 数据，使用 GetMemory 方法
                SecsFormat.Binary => item.GetMemory<byte>().ToArray(),

                // 默认情况使用 ToString()
                _ => item.ToString() ?? string.Empty
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "提取Item值失败，返回默认值 [设备: {EquipmentId}, 格式: {Format}]",
                _equipmentId, item.Format);
            return GetDefaultValueForFormat(item.Format);
        }
    }

    /// <summary>
    /// 安全提取字符串值的辅助方法
    /// </summary>
    private string ExtractStringValue(Item item)
    {
        try
        {
            // 方法1：使用 ToString() 方法（最安全）
            return item.ToString() ?? string.Empty;

            // 或者方法2：如果 FirstValue 支持可空类型，可以这样写：
            // var stringValue = item.FirstValue<string?>();
            // return stringValue ?? string.Empty;

            // 或者方法3：使用 GetFirstValueOrDefault（如果存在）
            // return item.GetFirstValueOrDefault<string>(string.Empty) ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// 获取格式对应的默认值
    /// </summary>
    private object GetDefaultValueForFormat(SecsFormat format)
    {
        return format switch
        {
            SecsFormat.U1 => (byte)0,
            SecsFormat.U2 => (ushort)0,
            SecsFormat.U4 => 0u,
            SecsFormat.I1 => (sbyte)0,
            SecsFormat.I2 => (short)0,
            SecsFormat.I4 => 0,
            SecsFormat.F4 => 0.0f,
            SecsFormat.F8 => 0.0,
            SecsFormat.ASCII => string.Empty,
            SecsFormat.Binary => Array.Empty<byte>(),
            _ => string.Empty
        };
    }


    /// <summary>
    /// 提取报告ID
    /// </summary>
    private int ExtractReportId(SecsMessage message)
    {
        try
        {
            // 从SECS消息中提取报告ID
            // 具体实现取决于消息格式，这里提供一个示例
            if (message.SecsItem?.Items != null && message.SecsItem.Items.Length > 0)
            {
                var firstItem = message.SecsItem.Items[0];
                if (firstItem.Format == SecsFormat.U2 || firstItem.Format == SecsFormat.U4)
                {
                    return (int)firstItem.FirstValue<uint>();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "提取报告ID失败，使用默认值 [设备: {EquipmentId}]", _equipmentId);
        }

        // 如果提取失败，返回默认值
        return 0;
    }

    /// <summary>
    /// 提取数据值
    /// </summary>
    private IDictionary<string, object> ExtractDataValues(SecsMessage message)
    {
        var dataValues = new Dictionary<string, object>();

        try
        {
            if (message.SecsItem?.Items != null)
            {
                // 遍历消息项并提取数据值
                for (int i = 0; i < message.SecsItem.Items.Length; i++)
                {
                    var item = message.SecsItem.Items[i];
                    var key = $"DV_{i + 1}"; // 生成数据变量键名
                    var value = ExtractItemValue(item);
                    dataValues[key] = value;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "提取数据值失败 [设备: {EquipmentId}]", _equipmentId);
        }

        return dataValues;
    }

    /// <summary>
    /// 获取默认数据变量
    /// </summary>
    private IEnumerable<uint> GetDefaultDataVariables()
    {
        // 返回默认监控的数据变量ID列表
        // 实际应从配置文件或数据库读取
        return new uint[] { 1, 2, 3, 4, 5 };
    }

    /// <summary>
    /// 获取默认事件
    /// </summary>
    private IEnumerable<uint> GetDefaultEvents()
    {
        // 返回默认监控的事件ID列表
        return new uint[] { 1, 2, 3 };
    }

    /// <summary>
    /// 获取默认报警ID
    /// </summary>
    private IEnumerable<uint> GetDefaultAlarmIds()
    {
        // 返回默认监控的报警ID列表
        return new uint[] { 1, 2, 3, 4, 5 };
    }

    /// <summary>
    /// 确定整体健康状态
    /// </summary>
    private HealthStatus DetermineOverallHealth(List<HealthCheckItem> checkItems)
    {
        if (checkItems.Any(item => item.Status == HealthStatus.Unhealthy))
            return HealthStatus.Unhealthy;

        if (checkItems.Any(item => item.Status == HealthStatus.Degraded))
            return HealthStatus.Degraded;

        return HealthStatus.Healthy;
    }

    /// <summary>
    /// 检查是否已释放
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(SecsDeviceService));
        }
    }

    #endregion

    #region 资源释放

    public void Dispose()
    {
        DisposeAsync().AsTask().Wait();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        _logger.LogInformation("释放设备服务资源 [设备: {EquipmentId}]", _equipmentId);

        try
        {
            // 停止服务
            await StopAsync("服务释放");

            // 取消订阅事件
            _hsmsClient.ConnectionStateChanged -= OnConnectionStateChanged;
            _hsmsClient.MessageReceived -= OnMessageReceived;
            _hsmsClient.MessageTimeout -= OnMessageTimeout;

            // 释放信号量
            _statusSemaphore?.Dispose();
            _operationSemaphore?.Dispose();
            _serviceCts?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "释放设备服务资源异常 [设备: {EquipmentId}]: {ErrorMessage}", _equipmentId, ex.Message);
        }

        _logger.LogInformation("设备服务资源已释放 [设备: {EquipmentId}]", _equipmentId);
    }

    #endregion
}

/// <summary>
/// 报警消息数据
/// </summary>
internal class AlarmMessageData
{
    public ushort AlarmId { get; set; }
    public string AlarmText { get; set; } = string.Empty;
    public AlarmSeverity Severity { get; set; }
    public bool IsSet { get; set; }
}
