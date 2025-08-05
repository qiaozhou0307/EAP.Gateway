using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using EAP.Gateway.Core.Events.Equipment;
using EAP.Gateway.Core.Events.Message;
using EAP.Gateway.Core.ValueObjects;
using EAP.Gateway.Infrastructure.Communications.SecsGem.Events;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Secs4Net;

namespace EAP.Gateway.Infrastructure.Communications.SecsGem;

/// <summary>
/// HSMS客户端实现，基于Secs4Net 2.4.1
/// 修复了API使用错误，正确使用Secs4Net的类型和构造函数
/// </summary>
public class HsmsClient : IHsmsClient
{
    private readonly EquipmentId _equipmentId;
    private readonly EquipmentConfiguration _configuration;
    private readonly ILogger<HsmsClient> _logger;
    private readonly IMediator _mediator;
    private readonly SemaphoreSlim _connectionSemaphore = new(1, 1);
    private readonly SemaphoreSlim _sendSemaphore = new(1, 1);
    private readonly CancellationTokenSource _disposeCts = new();
    private readonly Timer _heartbeatTimer;

    // 修复：使用正确的Secs4Net类型
    private Secs4Net.SecsGem? _secsGem;
    private Secs4Net.HsmsConnection? _hsmsConnection;
    private Core.ValueObjects.ConnectionState _connectionState;
    private Task? _messageProcessingTask;

    // 统计信息
    private int _messagesSent = 0;
    private int _messagesReceived = 0;
    private int _messageTimeouts = 0;
    private int _reconnectionAttempts = 0;
    private DateTime? _lastMessageSent;
    private DateTime? _lastMessageReceived;
    private DateTime? _lastHeartbeat;
    private long _bytesSent = 0;
    private long _bytesReceived = 0;
    private readonly List<double> _responseTimes = new();

    private volatile bool _disposed = false;
    private volatile bool _isHeartbeatActive = false;

    public EquipmentId EquipmentId => _equipmentId;
    public Core.ValueObjects.ConnectionState ConnectionState => _connectionState;

    //使用正确的 Secs4Net.ConnectionState 枚举值
    public Secs4Net.ConnectionState SecsConnectionState => _hsmsConnection?.State ?? Secs4Net.ConnectionState.Retry;
    public EquipmentConfiguration Configuration => _configuration;
    public bool IsConnected => _connectionState.IsConnected &&
    (SecsConnectionState == Secs4Net.ConnectionState.Selected ||
     SecsConnectionState == Secs4Net.ConnectionState.Connected);
    public bool IsDisposed => _disposed;
    public DateTime? LastHeartbeat => _lastHeartbeat;

    public event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;
    public event EventHandler<SecsMessageReceivedEventArgs>? MessageReceived;
    public event EventHandler<SecsMessageSentEventArgs>? MessageSent;
    public event EventHandler<MessageTimeoutEventArgs>? MessageTimeout;

    /// <summary>
    /// 构造函数
    /// </summary>
    public HsmsClient(
        EquipmentId equipmentId,
        EquipmentConfiguration configuration,
        IMediator mediator,
        ILogger<HsmsClient> logger)
    {
        _equipmentId = equipmentId ?? throw new ArgumentNullException(nameof(equipmentId));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _connectionState = Core.ValueObjects.ConnectionState.Initial();

        // 初始化心跳定时器（但不启动）
        _heartbeatTimer = new Timer(OnHeartbeatTimer, null, Timeout.Infinite, Timeout.Infinite);

        _logger.LogInformation("HSMS客户端已创建 [设备ID: {EquipmentId}, 端点: {Endpoint}]",
            _equipmentId, _configuration.Endpoint);
    }

    #region 连接管理

    /// <summary>
    /// 建立HSMS连接
    /// </summary>
    public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (IsConnected)
        {
            _logger.LogDebug("设备 {EquipmentId} 已连接，跳过连接操作", _equipmentId);
            return true;
        }

        await _connectionSemaphore.WaitAsync(cancellationToken);
        try
        {
            _logger.LogInformation("开始连接设备 {EquipmentId} [端点: {Endpoint}]", _equipmentId, _configuration.Endpoint);

            // 根据 EquipmentConfiguration 的实际结构，修复配置映射：

            var secsGemOptions = new Secs4Net.SecsGemOptions
            {
                IpAddress = _configuration.Endpoint.IpAddress,
                Port = _configuration.Endpoint.Port,

                // 修复：从嵌套的 Timeouts 对象获取超时配置
                T3 = _configuration.Timeouts.T3,
                T5 = _configuration.Timeouts.T5,
                T6 = _configuration.Timeouts.T6,
                T7 = _configuration.Timeouts.T7,
                T8 = _configuration.Timeouts.T8,

                // 修复：根据 ConnectionMode 枚举确定 IsActive
                IsActive = _configuration.ConnectionMode == ConnectionMode.Active,

                // 修复：从 HeartbeatInterval 属性获取心跳间隔（转换为毫秒）
                LinkTestInterval = _configuration.HeartbeatInterval * 1000, // 秒转换为毫秒

                // 如果 SecsGemOptions 需要 DeviceId，可以从设备ID或其他地方获取
                DeviceId = 1, // 或者使用固定值，具体取决于您的业务需求

                // 可选的缓冲区配置 - 使用合适的默认值
                SocketReceiveBufferSize = GetSocketReceiveBufferSize(),
                EncodeBufferInitialSize = GetEncodeBufferInitialSize()
            };

            var optionsWrapper = Options.Create(secsGemOptions);

            // 创建自定义日志适配器
            var secsLogger = new SecsGemLoggerAdapter(_logger);

            try
            {
                // 修复：使用正确的Secs4Net.HsmsConnection构造函数
                _hsmsConnection = new Secs4Net.HsmsConnection(optionsWrapper, secsLogger);
                _secsGem = new Secs4Net.SecsGem(optionsWrapper, _hsmsConnection, secsLogger);

                // 订阅连接状态变化事件
                _hsmsConnection.ConnectionChanged += OnConnectionChanged;

                // 启动HSMS连接
                await _hsmsConnection.StartAsync(cancellationToken);

                // 等待连接建立
                var connectTimeout = TimeSpan.FromMilliseconds(_configuration.Timeouts.T5);
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(connectTimeout);

                var connected = await WaitForConnectionAsync(timeoutCts.Token);

                if (connected)
                {
                    // 更新连接状态
                    var sessionId = GenerateSessionId();
                    var newConnectionState = Core.ValueObjects.ConnectionState.Connected(sessionId);
                    await UpdateConnectionStateAsync(newConnectionState, "成功建立HSMS连接");

                    // 启动消息处理
                    _messageProcessingTask = ProcessPrimaryMessagesAsync(_disposeCts.Token);

                    // 建立SECS/GEM通信
                    var commEstablished = await EstablishCommunicationAsync(timeoutCts.Token);
                    if (!commEstablished)
                    {
                        _logger.LogWarning("设备 {EquipmentId} HSMS连接成功但建立通信失败", _equipmentId);
                    }

                    // 启动心跳检测
                    await StartHeartbeatAsync(_disposeCts.Token);

                    _logger.LogInformation("设备 {EquipmentId} 连接成功 [会话ID: {SessionId}]", _equipmentId, sessionId);
                    return true;
                }
                else
                {
                    _logger.LogError("设备 {EquipmentId} 连接超时", _equipmentId);
                    await CleanupConnectionAsync();
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "设备 {EquipmentId} 连接失败: {ErrorMessage}", _equipmentId, ex.Message);
                await CleanupConnectionAsync();
                return false;
            }
        }
        finally
        {
            _connectionSemaphore.Release();
        }
    }

    /// <summary>
    /// 断开HSMS连接
    /// </summary>
    public async Task DisconnectAsync(string? reason = null, CancellationToken cancellationToken = default)
    {
        if (_disposed || !_connectionState.IsConnected)
        {
            return;
        }

        await _connectionSemaphore.WaitAsync(cancellationToken);
        try
        {
            _logger.LogInformation("断开设备 {EquipmentId} 连接 [原因: {Reason}]", _equipmentId, reason ?? "手动断开");

            // 停止心跳检测
            await StopHeartbeatAsync();

            // 更新连接状态
            var disconnectedState = Core.ValueObjects.ConnectionState.Disconnected();
            await UpdateConnectionStateAsync(disconnectedState, reason);

            // 清理连接资源
            await CleanupConnectionAsync();

            _logger.LogInformation("设备 {EquipmentId} 连接已断开", _equipmentId);
        }
        finally
        {
            _connectionSemaphore.Release();
        }
    }

    /// <summary>
    /// 重连设备
    /// </summary>
    public async Task<bool> ReconnectAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("重连设备 {EquipmentId}", _equipmentId);

        Interlocked.Increment(ref _reconnectionAttempts);

        // 先断开连接
        await DisconnectAsync("重连操作", cancellationToken);

        // 智能重试
        var attemptNumber = _connectionState.RetryCount;
        var delayMs = _configuration.RetryConfig.CalculateDelay(attemptNumber);
        await Task.Delay(delayMs, cancellationToken);

        // 尝试重新连接
        return await ConnectAsync(cancellationToken);
    }

    #endregion

    #region 消息发送接收

    /// <summary>
    /// 发送SECS消息并等待回复
    /// </summary>
    public async Task<SecsMessage?> SendAsync(SecsMessage message, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (!IsConnected)
        {
            throw new InvalidOperationException($"设备 {_equipmentId} 未连接");
        }

        if (_secsGem == null)
        {
            throw new InvalidOperationException("SecsGem实例未初始化");
        }

        await _sendSemaphore.WaitAsync(cancellationToken);
        try
        {
            var stopwatch = Stopwatch.StartNew();
            var messageLength = EstimateMessageSize(message);

            _logger.LogDebug("发送SECS消息 [设备: {EquipmentId}, 消息: {MessageType}]",
                _equipmentId, $"S{message.S}F{message.F}");

            // 发送消息并等待回复
            var response = await _secsGem.SendAsync(message, cancellationToken);

            stopwatch.Stop();

            // 更新统计信息
            Interlocked.Increment(ref _messagesSent);
            Interlocked.Add(ref _bytesSent, messageLength);
            _lastMessageSent = DateTime.UtcNow;

            lock (_responseTimes)
            {
                _responseTimes.Add(stopwatch.Elapsed.TotalMilliseconds);
                if (_responseTimes.Count > 100) // 保持最近100个响应时间
                {
                    _responseTimes.RemoveAt(0);
                }
            }

            // 触发消息发送事件
            MessageSent?.Invoke(this, new SecsMessageSentEventArgs(_equipmentId, message, messageLength));

            // 发布领域事件
            await _mediator.Publish(new SecsMessageSentEvent(
                _equipmentId, message.S, message.F, message.ReplyExpected,
                0, DateTime.UtcNow, (int)messageLength), cancellationToken);

            if (response != null)
            {
                // 处理收到的回复消息
                await ProcessReceivedMessageAsync(response);
            }

            return response;
        }
        catch (TimeoutException ex)
        {
            Interlocked.Increment(ref _messageTimeouts);

            _logger.LogWarning(ex, "设备 {EquipmentId} 消息发送超时 [消息: {MessageType}]",
                _equipmentId, $"S{message.S}F{message.F}");

            // 触发超时事件
            var timeoutArgs = new MessageTimeoutEventArgs(_equipmentId, message,
                DateTime.UtcNow.AddMilliseconds(-_configuration.Timeouts.T3),
                TimeSpan.FromMilliseconds(_configuration.Timeouts.T3));
            MessageTimeout?.Invoke(this, timeoutArgs);

            // 发布超时事件
            await _mediator.Publish(new Core.Events.Message.MessageTimeoutEvent(
                _equipmentId, message.S, message.F, 0,
                DateTime.UtcNow.AddMilliseconds(-_configuration.Timeouts.T3),
                DateTime.UtcNow), cancellationToken);

            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "设备 {EquipmentId} 消息发送失败 [消息: {MessageType}]: {ErrorMessage}",
                _equipmentId, $"S{message.S}F{message.F}", ex.Message);
            throw;
        }
        finally
        {
            _sendSemaphore.Release();
        }
    }

    /// <summary>
    /// 发送SECS消息（无需等待回复）
    /// </summary>
    public async Task SendWithoutReplyAsync(SecsMessage message, CancellationToken cancellationToken = default)
    {
        // 对于不需要回复的消息，直接调用SendAsync
        await SendAsync(message, cancellationToken);
    }

    /// <summary>
    /// 获取主要消息流
    /// </summary>
    public async IAsyncEnumerable<SecsMessage> GetPrimaryMessageAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (_secsGem == null)
        {
            yield break;
        }

        // 修复：正确处理 PrimaryMessageWrapper
        await foreach (var messageWrapper in _secsGem.GetPrimaryMessageAsync(cancellationToken))
        {
            // 从 PrimaryMessageWrapper 中提取 SecsMessage
            yield return messageWrapper.PrimaryMessage;
        }
    }

    #endregion

    #region 心跳检测

    /// <summary>
    /// 发送心跳消息（S1F1）
    /// </summary>
    public async Task<bool> SendHeartbeatAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConnected || _disposed)
            return false;

        try
        {
            // 修复：使用正确的 SecsMessage 构造方式
            var heartbeatMessage = new SecsMessage(1, 1, replyExpected: true);
            // S1F1 通常不需要数据项，但如果需要可以设置：
            // heartbeatMessage.SecsItem = Item.L();

            var response = await SendAsync(heartbeatMessage, cancellationToken);

            var success = response != null;
            if (success)
            {
                _lastHeartbeat = DateTime.UtcNow;
                _logger.LogTrace("设备 {EquipmentId} 心跳发送成功", _equipmentId);
            }
            else
            {
                _logger.LogWarning("设备 {EquipmentId} 心跳发送失败", _equipmentId);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "设备 {EquipmentId} 发送心跳异常: {ErrorMessage}", _equipmentId, ex.Message);
            return false;
        }
    }

    /// <summary>
    /// 启动自动心跳检测
    /// </summary>
    public async Task StartHeartbeatAsync(CancellationToken cancellationToken = default)
    {
        if (_isHeartbeatActive || _disposed)
        {
            return;
        }

        _isHeartbeatActive = true;
        var interval = TimeSpan.FromSeconds(_configuration.HeartbeatInterval);
        if (interval <= TimeSpan.Zero)
        {
            _logger.LogWarning("设备 {EquipmentId} 心跳间隔配置无效，无法启动心跳检测", _equipmentId);
            return;
        }

        _logger.LogInformation("启动设备 {EquipmentId} 心跳检测 [间隔: {Interval}ms]", _equipmentId, interval.TotalMilliseconds);

        _heartbeatTimer.Change(interval, interval);

        await Task.CompletedTask;
    }

    /// <summary>
    /// 停止自动心跳检测
    /// </summary>
    public async Task StopHeartbeatAsync()
    {
        if (!_isHeartbeatActive)
        {
            return;
        }

        _isHeartbeatActive = false;
        _heartbeatTimer.Change(Timeout.Infinite, Timeout.Infinite);

        _logger.LogInformation("停止设备 {EquipmentId} 心跳检测", _equipmentId);

        await Task.CompletedTask;
    }

    /// <summary>
    /// 心跳定时器回调
    /// </summary>
    private async void OnHeartbeatTimer(object? state)
    {
        if (!_isHeartbeatActive || _disposed)
        {
            return;
        }

        try
        {
            var success = await SendHeartbeatAsync(_disposeCts.Token);
            if (!success)
            {
                _logger.LogWarning("设备 {EquipmentId} 心跳检测失败，可能需要重连", _equipmentId);

                // 连续心跳失败可能需要重连
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await ReconnectAsync(_disposeCts.Token);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "设备 {EquipmentId} 重连失败: {ErrorMessage}", _equipmentId, ex.Message);
                    }
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "设备 {EquipmentId} 心跳检测异常: {ErrorMessage}", _equipmentId, ex.Message);
        }
    }

    #endregion

    #region 测试和诊断

    /// <summary>
    /// 测试连接
    /// </summary>
    public async Task<Core.ValueObjects.ConnectionTestResult> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
        {
            return Core.ValueObjects.ConnectionTestResult.Failure("设备未连接");
        }

        var stopwatch = Stopwatch.StartNew();
        try
        {
            // 修复：使用正确的SecsMessage构造函数
            var testMessage = new SecsMessage(1, 13, replyExpected: true);
            var response = await SendAsync(testMessage, cancellationToken);

            stopwatch.Stop();

            if (response != null)
            {
                return Core.ValueObjects.ConnectionTestResult.Success(stopwatch.Elapsed);
            }
            else
            {
                return Core.ValueObjects.ConnectionTestResult.Failure("未收到响应", stopwatch.Elapsed);
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return Core.ValueObjects.ConnectionTestResult.Failure(ex.Message, stopwatch.Elapsed);
        }
    }

    /// <summary>
    /// 获取设备状态（S1F3）
    /// </summary>
    public async Task<EquipmentState?> GetEquipmentStateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // 修复：分步创建消息
            var statusRequestMessage = new SecsMessage(1, 3, replyExpected: true);
            statusRequestMessage.SecsItem = Item.L(
                Item.U4(1), // SVID for equipment state
                Item.U4(2)  // SVID for sub-state (optional)
            );

            var response = await SendAsync(statusRequestMessage, cancellationToken);

            if (response != null && response.S == 1 && response.F == 4)
            {
                return ParseEquipmentStateFromS1F4(response);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "设备 {EquipmentId} 获取状态失败: {ErrorMessage}", _equipmentId, ex.Message);
            return null;
        }
    }
    /// <summary>
    /// 建立通信请求（S1F13）
    /// </summary>
    public async Task<bool> EstablishCommunicationAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // 修复：分步创建消息
            var establishCommMessage = new SecsMessage(1, 13, replyExpected: true);
            establishCommMessage.SecsItem = Item.L(); // S1F13 通常包含空列表

            var response = await SendAsync(establishCommMessage, cancellationToken);

            if (response != null && response.S == 1 && response.F == 14)
            {
                // 解析 S1F14 响应
                var success = ParseS1F14Response(response);
                _logger.LogInformation("设备 {EquipmentId} 建立通信{Result}",
                    _equipmentId, success ? "成功" : "失败");
                return success;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "设备 {EquipmentId} 建立通信失败: {ErrorMessage}", _equipmentId, ex.Message);
            return false;
        }
    }
    /// <summary>
    /// 发送远程命令（S2F41）
    /// </summary>
    public async Task<bool> SendRemoteCommandAsync(string commandName, Dictionary<string, object>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 修复：分步创建消息
            var commandMessage = new SecsMessage(2, 41, replyExpected: true);

            // 构建命令数据结构
            var commandData = new List<Item>
        {
            Item.A(commandName) // 命令名称
        };

            // 添加参数（如果有）
            if (parameters != null && parameters.Count > 0)
            {
                var parameterItems = parameters.Select(kvp =>
                    Item.L(
                        Item.A(kvp.Key),
                        ConvertValueToItem(kvp.Value)
                    )).ToArray();

                commandData.Add(Item.L(parameterItems));
            }

            commandMessage.SecsItem = Item.L(commandData.ToArray());

            var response = await SendAsync(commandMessage, cancellationToken);

            if (response != null && response.S == 2 && response.F == 42)
            {
                // 解析 S2F42 响应（CMDACK）
                var commandAck = ParseCommandAcknowledgment(response);
                var success = commandAck == 0; // 0表示接受

                _logger.LogInformation("设备 {EquipmentId} 远程命令 {Command} {Result} [CMDACK: {Ack}]",
                    _equipmentId, commandName, success ? "成功" : "失败", commandAck);

                return success;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "设备 {EquipmentId} 发送远程命令 {Command} 失败: {ErrorMessage}",
                _equipmentId, commandName, ex.Message);
            return false;
        }
    }

    ///// <summary>
    ///// 数据采集请求（S6F19）
    ///// </summary>
    //public async Task<bool> RequestDataCollectionAsync(IEnumerable<uint> dataVariableIds,
    //    CancellationToken cancellationToken = default)
    //{
    //    try
    //    {
    //        // 修复：分步创建消息
    //        var dataRequestMessage = new SecsMessage(6, 19, replyExpected: true);

    //        // 构建数据变量ID列表
    //        var rptid = Item.U4(1); // Report ID
    //        var dvItems = dataVariableIds.Select(id => Item.U4(id)).ToArray();

    //        dataRequestMessage.SecsItem = Item.L(
    //            rptid,
    //            Item.L(dvItems)
    //        );

    //        var response = await SendAsync(dataRequestMessage, cancellationToken);

    //        if (response != null && response.S == 6 && response.F == 20)
    //        {
    //            // 处理 S6F20 响应
    //            return ProcessDataCollectionResponse(response);
    //        }

    //        return false;
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "设备 {EquipmentId} 请求数据采集失败: {ErrorMessage}", _equipmentId, ex.Message);
    //        return false;
    //    }
    //}


    /// <summary>
    /// 获取连接统计信息
    /// </summary>
    public Core.ValueObjects.ConnectionStatistics GetConnectionStatistics()
    {
        double averageResponseTime;
        lock (_responseTimes)
        {
            averageResponseTime = _responseTimes.Count > 0 ? _responseTimes.Average() : 0;
        }

        return new Core.ValueObjects.ConnectionStatistics(
            uptime: _connectionState.ConnectionDuration,
            messagesSent: _messagesSent,
            messagesReceived: _messagesReceived,
            messageTimeouts: _messageTimeouts,
            reconnectionAttempts: _reconnectionAttempts,
            lastMessageSent: _lastMessageSent,
            lastMessageReceived: _lastMessageReceived,
            lastHeartbeat: _lastHeartbeat,
            averageResponseTime: averageResponseTime,
            bytesSent: _bytesSent,
            bytesReceived: _bytesReceived
        );
    }

    #endregion

    #region 私有辅助方法

    /// <summary>
    /// 创建标准SECS消息的辅助方法
    /// </summary>
    /// <param name="s">Stream</param>
    /// <param name="f">Function</param>
    /// <param name="replyExpected">是否期望回复</param>
    /// <param name="data">消息数据项</param>
    /// <returns>SECS消息</returns>
    private SecsMessage CreateSecsMessage(byte s, byte f, bool replyExpected = true, Item? data = null)
    {
        var message = new SecsMessage(s, f, replyExpected);
        if (data != null)
        {
            message.SecsItem = data;
        }
        return message;
    }

    /// <summary>
    /// 创建带名称的SECS消息（用于调试）
    /// </summary>
    /// <param name="s">Stream</param>
    /// <param name="f">Function</param>
    /// <param name="name">消息名称</param>
    /// <param name="replyExpected">是否期望回复</param>
    /// <param name="data">消息数据项</param>
    /// <returns>SECS消息</returns>
    private SecsMessage CreateNamedSecsMessage(byte s, byte f, string name, bool replyExpected = true, Item? data = null)
    {
        var message = new SecsMessage(s, f, replyExpected);
        message.Name = name; // 设置消息名称用于调试
        if (data != null)
        {
            message.SecsItem = data;
        }
        return message;
    }


    // ====================================================================================
    // 4. 值类型转换辅助方法
    // ====================================================================================

    /// <summary>
    /// 将C#值转换为SECS Item
    /// </summary>
    /// <param name="value">要转换的值</param>
    /// <returns>SECS Item</returns>
    private Item ConvertValueToItem(object value)
    {
        return value switch
        {
            string str => Item.A(str),
            int i => Item.I4(i),
            uint ui => Item.U4(ui),
            short s => Item.I2(s),
            ushort us => Item.U2(us),
            byte b => Item.U1(b),
            sbyte sb => Item.I1(sb),
            long l => Item.I8(l),
            ulong ul => Item.U8(ul),
            float f => Item.F4(f),
            double d => Item.F8(d),
            bool b => Item.Boolean(b),
            byte[] bytes => Item.B(bytes),
            _ => Item.A(value.ToString() ?? "")
        };
    }

    // ====================================================================================
    // 5. 响应解析辅助方法
    // ====================================================================================

    /// <summary>
    /// 解析S1F14建立通信响应
    /// </summary>
    /// <param name="response">响应消息</param>
    /// <returns>是否成功建立通信</returns>
    private bool ParseS1F14Response(SecsMessage response)
    {
        try
        {
            // S1F14 通常返回COMMACK值
            if (response.SecsItem != null)
            {
                var commAck = response.SecsItem.FirstValue<byte>();
                return commAck == 0; // 0表示接受
            }
            return true; // 如果没有数据项，认为成功
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 解析命令确认响应（S2F42）
    /// </summary>
    /// <param name="response">响应消息</param>
    /// <returns>命令确认码</returns>
    private byte ParseCommandAcknowledgment(SecsMessage response)
    {
        try
        {
            if (response.SecsItem != null)
            {
                return response.SecsItem.FirstValue<byte>();
            }
            return 1; // 默认为拒绝
        }
        catch
        {
            return 1; // 解析失败，默认为拒绝
        }
    }

    /// <summary>
    /// 从S1F4响应解析设备状态
    /// </summary>
    /// <param name="response">响应消息</param>
    /// <returns>设备状态</returns>
    private EquipmentState? ParseEquipmentStateFromS1F4(SecsMessage response)
    {
        try
        {
            if (response.SecsItem != null && response.SecsItem.Count > 0)
            {
                var stateValue = response.SecsItem[0].FirstValue<byte>();
                return (EquipmentState)stateValue;
            }
            return null;
        }
        catch
        {
            return null;
        }
    }


    private async Task<bool> WaitForConnectionAsync(CancellationToken cancellationToken)
    {
        const int checkIntervalMs = 100;
        const int maxChecks = 100; // 最多等待10秒

        for (int i = 0; i < maxChecks && !cancellationToken.IsCancellationRequested; i++)
        {
            // 修改：检查 Selected 状态而不是其他状态
            if (SecsConnectionState == Secs4Net.ConnectionState.Selected)
            {
                return true;
            }

            await Task.Delay(checkIntervalMs, cancellationToken);
        }

        return false;
    }

    /// <summary>
    /// 连接状态变化事件处理
    /// </summary>
    private async void OnConnectionChanged(object? sender, Secs4Net.ConnectionState e)
    {
        if (_disposed)
            return;

        try
        {
            var previousState = _connectionState;
            var secsState = e; // 直接使用事件参数

            _logger.LogDebug("设备 {EquipmentId} SECS连接状态变化: {NewState}", _equipmentId, secsState);

            // 根据SECS连接状态更新领域连接状态
            var newConnectionState = secsState switch
            {
                Secs4Net.ConnectionState.Selected => previousState.IsConnected
                    ? previousState
                    : Core.ValueObjects.ConnectionState.Connected(GenerateSessionId()),
                Secs4Net.ConnectionState.Connected => previousState.IsConnected
                    ? previousState
                    : Core.ValueObjects.ConnectionState.Connected(GenerateSessionId()),
                Secs4Net.ConnectionState.Connecting => previousState,
                Secs4Net.ConnectionState.Retry => previousState.IsConnected
                    ? previousState.Disconnect("需要重试连接")
                    : Core.ValueObjects.ConnectionState.Disconnected(),
                _ => previousState
            };

            if (!newConnectionState.Equals(previousState))
            {
                await UpdateConnectionStateAsync(newConnectionState, $"SECS状态变化: {secsState}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理设备 {EquipmentId} 连接状态变化异常: {ErrorMessage}", _equipmentId, ex.Message);
        }
    }

    /// <summary>
    /// 更新连接状态 - 修复版本
    /// </summary>
    private async Task UpdateConnectionStateAsync(Core.ValueObjects.ConnectionState newState, string? reason)
    {
        var previousState = _connectionState;
        _connectionState = newState;

        // 正确传递参数给 ConnectionStateChangedEventArgs 构造函数
        ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs(
            _equipmentId,
            previousState,
            newState,
            reason,
            newState.SessionId));  // 使用 newState.SessionId 而不是 SecsConnectionState

        // 发布领域事件
        if (newState.IsConnected && !previousState.IsConnected)
        {
            await _mediator.Publish(new EquipmentConnectedEvent(
                _equipmentId, newState.LastConnectedAt ?? DateTime.UtcNow,
                newState.SessionId ?? "Unknown", _configuration.Endpoint));
        }
        else if (!newState.IsConnected && previousState.IsConnected)
        {
            await _mediator.Publish(new EquipmentDisconnectedEvent(
                _equipmentId, newState.LastDisconnectedAt ?? DateTime.UtcNow,
                reason,
                DetermineDisconnectionType(reason),
                previousState.SessionId,
                previousState.ConnectionDuration));
        }
    }

    /// <summary>
    /// 处理接收到的主要消息
    /// </summary>
    private async Task ProcessPrimaryMessagesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var message in GetPrimaryMessageAsync(cancellationToken))
            {
                await ProcessReceivedMessageAsync(message);
            }
        }
        catch (OperationCanceledException)
        {
            // 正常取消，不记录错误
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "设备 {EquipmentId} 处理主要消息异常: {ErrorMessage}", _equipmentId, ex.Message);
        }
    }

    /// <summary>
    /// 处理接收到的消息
    /// </summary>
    private async Task ProcessReceivedMessageAsync(SecsMessage message)
    {
        var messageLength = EstimateMessageSize(message);

        Interlocked.Increment(ref _messagesReceived);
        Interlocked.Add(ref _bytesReceived, messageLength);
        _lastMessageReceived = DateTime.UtcNow;

        // 触发消息接收事件
        MessageReceived?.Invoke(this, new SecsMessageReceivedEventArgs(_equipmentId, message, messageLength));

        // 发布领域事件
        await _mediator.Publish(new SecsMessageReceivedEvent(
            _equipmentId, message.S, message.F, message.ReplyExpected,
            0, DateTime.UtcNow, (int)messageLength));

        _logger.LogDebug("收到设备 {EquipmentId} 消息 [消息: {MessageType}]",
            _equipmentId, $"S{message.S}F{message.F}");
    }

    /// <summary>
    /// 清理连接资源
    /// </summary>
    private async Task CleanupConnectionAsync()
    {
        try
        {
            if (_hsmsConnection != null)
            {
                _hsmsConnection.ConnectionChanged -= OnConnectionChanged;
                await _hsmsConnection.StopAsync(CancellationToken.None);
                await _hsmsConnection.DisposeAsync();
                _hsmsConnection = null;
            }

            _secsGem?.Dispose();
            _secsGem = null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "设备 {EquipmentId} 清理连接资源异常: {ErrorMessage}", _equipmentId, ex.Message);
        }
    }

    /// <summary>
    /// 生成会话ID
    /// </summary>
    private string GenerateSessionId()
    {
        return $"{_equipmentId}_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid().ToString("N")[..8]}";
    }

    /// <summary>
    /// 估算消息大小
    /// </summary>
    private long EstimateMessageSize(SecsMessage message)
    {
        // 简单估算，实际应该基于SECS-II编码规则
        return 100; // 默认100字节
    }

    /// <summary>
    /// 从响应中解析设备状态
    /// </summary>
    private EquipmentState? ParseEquipmentStateFromResponse(SecsMessage response)
    {
        // 根据具体设备响应格式解析状态
        // 这里返回默认状态，实际应根据设备协议实现
        return EquipmentState.IDLE;
    }

    /// <summary>
    /// 确定断开连接类型
    /// </summary>
    private DisconnectionType DetermineDisconnectionType(string? reason)
    {
        if (string.IsNullOrEmpty(reason))
            return DisconnectionType.Unexpected;

        return reason.ToLowerInvariant() switch
        {
            var r when r.Contains("手动") || r.Contains("manual") => DisconnectionType.Manual,
            var r when r.Contains("网络") || r.Contains("network") => DisconnectionType.NetworkError,
            var r when r.Contains("超时") || r.Contains("timeout") => DisconnectionType.Timeout,
            var r when r.Contains("协议") || r.Contains("protocol") => DisconnectionType.ProtocolError,
            _ => DisconnectionType.Unexpected
        };
    }

    /// <summary>
    /// 获取Socket接收缓冲区大小
    /// </summary>
    private int GetSocketReceiveBufferSize()
    {
        // 如果通信配置存在且有最大消息大小设置，使用它作为缓冲区大小
        if (_configuration.CommunicationConfig?.MaxMessageSize > 0)
        {
            // 缓冲区应该比最大消息大小稍大，以容纳消息头等额外数据
            return Math.Max(4096, _configuration.CommunicationConfig.MaxMessageSize + 1024);
        }

        // 默认使用64KB缓冲区
        return 65536;
    }

    /// <summary>
    /// 获取编码缓冲区初始大小
    /// </summary>
    private int GetEncodeBufferInitialSize()
    {
        // 如果通信配置存在，基于其配置计算合适的初始大小
        if (_configuration.CommunicationConfig != null)
        {
            // 基于最大消息大小的1/4作为初始编码缓冲区大小，最小1KB
            var calculatedSize = Math.Max(1024, _configuration.CommunicationConfig.MaxMessageSize / 4);
            return Math.Min(calculatedSize, 16384); // 最大不超过16KB
        }

        // 默认使用4KB初始编码缓冲区
        return 4096;
    }

    /// <summary>
    /// 检查是否已释放
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(HsmsClient));
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

        _logger.LogInformation("释放设备 {EquipmentId} HSMS客户端资源", _equipmentId);

        // 停止心跳检测
        await StopHeartbeatAsync();

        // 断开连接
        await DisconnectAsync("资源释放");

        // 取消所有异步操作
        _disposeCts.Cancel();

        // 等待消息处理任务完成
        if (_messageProcessingTask != null)
        {
            try
            {
                await _messageProcessingTask;
            }
            catch
            {
                // 忽略取消异常
            }
        }

        // 释放资源
        _heartbeatTimer?.Dispose();
        _connectionSemaphore?.Dispose();
        _sendSemaphore?.Dispose();
        _disposeCts?.Dispose();

        _logger.LogInformation("设备 {EquipmentId} HSMS客户端资源已释放", _equipmentId);
    }

    #endregion
}

/// <summary>
/// Secs4Net日志适配器，将Secs4Net的日志适配到Microsoft.Extensions.Logging
/// </summary>
internal class SecsGemLoggerAdapter : Secs4Net.ISecsGemLogger
{
    private readonly ILogger _logger;

    public SecsGemLoggerAdapter(ILogger logger)
    {
        _logger = logger;
    }

    public void Debug(string msg, Exception? ex = null)
    {
        _logger.LogDebug(ex, "{Message}", msg);
    }

    public void Error(string msg, Exception? ex = null)
    {
        _logger.LogError(ex, "{Message}", msg);
    }

    public void Info(string msg, Exception? ex = null)
    {
        _logger.LogInformation(ex, "{Message}", msg);
    }

    public void Warning(string msg, Exception? ex = null)
    {
        _logger.LogWarning(ex, "{Message}", msg);
    }
}
