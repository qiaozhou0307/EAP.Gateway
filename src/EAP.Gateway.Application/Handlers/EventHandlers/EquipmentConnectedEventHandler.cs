using MediatR;
using Microsoft.Extensions.Logging;
using EAP.Gateway.Core.Events.Equipment;
using EAP.Gateway.Core.Repositories;
using EAP.Gateway.Core.ValueObjects;

namespace EAP.Gateway.Application.Handlers.EventHandlers;

/// <summary>
/// 设备连接事件处理器（最终修复版本）
/// 处理 EquipmentConnectedEvent 事件，更新缓存并发布到消息队列
/// </summary>
public class EquipmentConnectedEventHandler : INotificationHandler<EquipmentConnectedEvent>
{
    private readonly IDeviceStatusCacheService _cacheService;
    private readonly IKafkaProducerService _kafkaProducer;
    private readonly ILogger<EquipmentConnectedEventHandler> _logger;

    public EquipmentConnectedEventHandler(
        IDeviceStatusCacheService cacheService,
        IKafkaProducerService kafkaProducer,
        ILogger<EquipmentConnectedEventHandler> logger)
    {
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _kafkaProducer = kafkaProducer ?? throw new ArgumentNullException(nameof(kafkaProducer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Handle(EquipmentConnectedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("处理设备连接事件: {EquipmentId}, 端点: {Endpoint}, 会话: {SessionId}, 重连: {IsReconnection}",
                notification.EquipmentId.Value,
                notification.Endpoint,
                notification.SessionId,
                notification.IsReconnection);

            // 更新缓存中的设备状态
            await UpdateDeviceConnectionCacheAsync(notification, cancellationToken);

            // 发布到Kafka
            await PublishConnectionEventAsync(notification, cancellationToken);

            _logger.LogInformation("设备连接事件处理完成: {EquipmentId}", notification.EquipmentId.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理设备连接事件失败: {EquipmentId}", notification.EquipmentId.Value);
            throw;
        }
    }

    /// <summary>
    /// 更新设备连接缓存
    /// </summary>
    private async Task UpdateDeviceConnectionCacheAsync(EquipmentConnectedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var currentStatus = await _cacheService.GetEquipmentStatusAsync(notification.EquipmentId, cancellationToken);

            if (currentStatus != null)
            {
                // 创建连接状态
                var connectedState = ConnectionState.Connected(
                    sessionId: notification.SessionId,
                    connectionTime: notification.ConnectedAt);

                // 不可变更新方式
                var updatedStatus = currentStatus
                    .WithConnectionState(connectedState)
                    .WithLastDataUpdate(notification.ConnectedAt);

                await _cacheService.SetEquipmentStatusAsync(updatedStatus, cancellationToken);

                _logger.LogDebug("已更新设备连接状态缓存: {EquipmentId}", notification.EquipmentId.Value);
            }
            else
            {
                _logger.LogWarning("设备状态缓存不存在，无法更新连接状态: {EquipmentId}", notification.EquipmentId.Value);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新设备连接状态缓存失败: {EquipmentId}", notification.EquipmentId.Value);
            throw;
        }
    }

    /// <summary>
    /// 发布连接事件到Kafka
    /// </summary>
    private async Task PublishConnectionEventAsync(EquipmentConnectedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            // 修复：正确访问 Endpoint 的属性
            var eventData = new
            {
                EquipmentId = notification.EquipmentId.Value,
                IpAddress = notification.Endpoint.IpAddress,        // 通过 Endpoint 访问
                Port = notification.Endpoint.Port,                  // 通过 Endpoint 访问
                ConnectedAt = notification.ConnectedAt,
                SessionId = notification.SessionId,
                IsReconnection = notification.IsReconnection,
                AttemptNumber = notification.AttemptNumber,
                ConnectionDurationMs = notification.ConnectionDurationMs,
                PreviousState = notification.PreviousState?.ToString(),
                AdditionalInfo = notification.AdditionalInfo,
                EventId = notification.EventId,
                OccurredOn = notification.OccurredOn
            };

            await _kafkaProducer.PublishEquipmentEventAsync(
                notification.EquipmentId.Value,
                "EquipmentConnected",
                eventData,
                cancellationToken);

            _logger.LogDebug("已发布设备连接事件到Kafka: {EquipmentId}, EventId: {EventId}",
                notification.EquipmentId.Value, notification.EventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发布设备连接事件到Kafka失败: {EquipmentId}", notification.EquipmentId.Value);
            throw;
        }
    }
}
