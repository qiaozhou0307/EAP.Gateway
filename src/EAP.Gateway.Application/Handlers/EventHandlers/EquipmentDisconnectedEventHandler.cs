using MediatR;
using Microsoft.Extensions.Logging;
using EAP.Gateway.Core.Events.Equipment;
using EAP.Gateway.Core.Repositories;
using EAP.Gateway.Core.ValueObjects;

namespace EAP.Gateway.Application.Handlers.EventHandlers;

/// <summary>
/// 设备断开连接事件处理器（最终版本）
/// 处理 EquipmentDisconnectedEvent 事件
/// </summary>
public class EquipmentDisconnectedEventHandler : INotificationHandler<EquipmentDisconnectedEvent>
{
    private readonly IDeviceStatusCacheService _cacheService;
    private readonly IKafkaProducerService _kafkaProducer;
    private readonly ILogger<EquipmentDisconnectedEventHandler> _logger;

    public EquipmentDisconnectedEventHandler(
        IDeviceStatusCacheService cacheService,
        IKafkaProducerService kafkaProducer,
        ILogger<EquipmentDisconnectedEventHandler> logger)
    {
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _kafkaProducer = kafkaProducer ?? throw new ArgumentNullException(nameof(kafkaProducer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Handle(EquipmentDisconnectedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("处理设备断开连接事件: {EquipmentId}, 原因: {Reason}, 类型: {DisconnectionType}, 需要重连: {RequiresReconnection}",
                notification.EquipmentId.Value,
                notification.Reason ?? "未指定",
                notification.DisconnectionType,
                notification.RequiresReconnection);

            // 更新缓存中的设备状态
            await UpdateDeviceDisconnectionCacheAsync(notification, cancellationToken);

            // 发布到Kafka
            await PublishDisconnectionEventAsync(notification, cancellationToken);

            _logger.LogInformation("设备断开连接事件处理完成: {EquipmentId}", notification.EquipmentId.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理设备断开连接事件失败: {EquipmentId}", notification.EquipmentId.Value);
            throw;
        }
    }

    /// <summary>
    /// 更新设备断开连接缓存
    /// </summary>
    private async Task UpdateDeviceDisconnectionCacheAsync(EquipmentDisconnectedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var currentStatus = await _cacheService.GetEquipmentStatusAsync(notification.EquipmentId, cancellationToken);

            if (currentStatus != null)
            {
                // 创建断开连接状态
                var disconnectedState = ConnectionState.Disconnected()
                    .Disconnect(notification.Reason, notification.DisconnectedAt);

                // 不可变更新
                var updatedStatus = currentStatus
                    .WithConnectionState(disconnectedState)
                    .WithLastDataUpdate(notification.DisconnectedAt);

                await _cacheService.SetEquipmentStatusAsync(updatedStatus, cancellationToken);

                _logger.LogDebug("已更新设备断开连接状态缓存: {EquipmentId}", notification.EquipmentId.Value);
            }
            else
            {
                _logger.LogWarning("设备状态缓存不存在，无法更新断开状态: {EquipmentId}", notification.EquipmentId.Value);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新设备断开连接状态缓存失败: {EquipmentId}", notification.EquipmentId.Value);
            throw;
        }
    }

    /// <summary>
    /// 发布断开连接事件到Kafka
    /// </summary>
    private async Task PublishDisconnectionEventAsync(EquipmentDisconnectedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var eventData = new
            {
                EquipmentId = notification.EquipmentId.Value,
                DisconnectedAt = notification.DisconnectedAt,
                Reason = notification.Reason,
                DisconnectionType = notification.DisconnectionType.ToString(),
                PreviousSessionId = notification.PreviousSessionId,
                ConnectionDuration = notification.ConnectionDuration?.TotalSeconds,
                IsExpectedDisconnection = notification.IsExpectedDisconnection,
                RequiresReconnection = notification.RequiresReconnection,
                PreviousState = notification.PreviousState?.ToString(),
                AdditionalInfo = notification.AdditionalInfo,
                EventId = notification.EventId,
                OccurredOn = notification.OccurredOn
            };

            await _kafkaProducer.PublishEquipmentEventAsync(
                notification.EquipmentId.Value,
                "EquipmentDisconnected",
                eventData,
                cancellationToken);

            _logger.LogDebug("已发布设备断开连接事件到Kafka: {EquipmentId}, EventId: {EventId}",
                notification.EquipmentId.Value, notification.EventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发布设备断开连接事件到Kafka失败: {EquipmentId}", notification.EquipmentId.Value);
            throw;
        }
    }
}
