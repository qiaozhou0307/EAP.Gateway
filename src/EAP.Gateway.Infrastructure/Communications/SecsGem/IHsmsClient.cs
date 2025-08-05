using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using EAP.Gateway.Core.ValueObjects;
using EAP.Gateway.Infrastructure.Communications.SecsGem.Events;
using Secs4Net;
using ConnectionState = EAP.Gateway.Core.ValueObjects.ConnectionState;
using EAP.Gateway.Core.Events.Equipment;

namespace EAP.Gateway.Infrastructure.Communications.SecsGem;

/// <summary>
/// HSMS客户端接口，基于Secs4Net 2.4.1实现SECS/GEM通信
/// 负责设备连接管理、消息发送接收、心跳检测等核心功能
/// </summary>
public interface IHsmsClient : IAsyncDisposable, IDisposable
{
    /// <summary>
    /// 设备标识
    /// </summary>
    EquipmentId EquipmentId { get; }

    /// <summary>
    /// 当前连接状态（使用领域模型中的ConnectionState）
    /// </summary>
    ConnectionState ConnectionState { get; }

    /// <summary>
    /// SECS/GEM通信状态（使用Secs4Net的ConnectionState）
    /// </summary>
    Secs4Net.ConnectionState SecsConnectionState { get; }

    /// <summary>
    /// 设备配置信息
    /// </summary>
    EquipmentConfiguration Configuration { get; }

    /// <summary>
    /// 是否已连接
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// 是否已释放资源
    /// </summary>
    bool IsDisposed { get; }

    /// <summary>
    /// 最后心跳时间
    /// </summary>
    DateTime? LastHeartbeat { get; }

    /// <summary>
    /// 连接状态变化事件（使用Core中的事件参数）
    /// 当HSMS连接状态发生变化时触发，用于同步更新Equipment聚合根的连接状态
    /// </summary>
    event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;

    /// <summary>
    /// 消息接收事件（使用Infrastructure中的事件参数）
    /// 当收到SECS消息时触发，用于处理设备数据和状态更新
    /// </summary>
    event EventHandler<SecsMessageReceivedEventArgs>? MessageReceived;

    /// <summary>
    /// 消息发送事件（使用Infrastructure中的事件参数）
    /// 当发送SECS消息时触发，用于记录通信日志和监控
    /// </summary>
    event EventHandler<SecsMessageSentEventArgs>? MessageSent;

    /// <summary>
    /// 消息超时事件（使用Infrastructure中的事件参数）
    /// 当消息发送超时时触发，用于错误处理和重试机制
    /// </summary>
    event EventHandler<MessageTimeoutEventArgs>? MessageTimeout;

    /// <summary>
    /// 建立HSMS连接
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>连接是否成功</returns>
    Task<bool> ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 断开HSMS连接
    /// </summary>
    /// <param name="reason">断开原因</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>断开连接任务</returns>
    Task DisconnectAsync(string? reason = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 发送SECS消息
    /// </summary>
    /// <param name="message">要发送的SECS消息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>响应消息，如果是不需要回复的消息则返回null</returns>
    Task<SecsMessage?> SendAsync(SecsMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// 发送SECS消息（无需等待回复）
    /// </summary>
    /// <param name="message">要发送的SECS消息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>发送任务</returns>
    Task SendWithoutReplyAsync(SecsMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取主要消息流（用于接收设备主动发送的消息）
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>消息异步枚举</returns>
    IAsyncEnumerable<SecsMessage> GetPrimaryMessageAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 发送心跳消息（LinkTest）
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>心跳是否成功</returns>
    Task<bool> SendHeartbeatAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 开始自动心跳检测
    /// 根据设备配置的LinkTestInterval定期发送心跳消息
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>心跳任务</returns>
    Task StartHeartbeatAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 停止自动心跳检测
    /// </summary>
    Task StopHeartbeatAsync();

    /// <summary>
    /// 测试连接（返回Core中的值对象）
    /// 发送S1F13请求验证通信是否正常
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>连接测试结果</returns>
    Task<ConnectionTestResult> TestConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取设备状态
    /// 发送S1F1请求获取设备当前状态
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>设备状态</returns>
    Task<EquipmentState?> GetEquipmentStateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 发送建立通信请求
    /// 发送S1F13建立通信，设备应回复S1F14
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>建立通信是否成功</returns>
    Task<bool> EstablishCommunicationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 重连设备
    /// 断开当前连接并尝试重新连接
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>重连是否成功</returns>
    Task<bool> ReconnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取连接统计信息（返回Core中的值对象）
    /// </summary>
    /// <returns>连接统计信息</returns>
    ConnectionStatistics GetConnectionStatistics();
}
