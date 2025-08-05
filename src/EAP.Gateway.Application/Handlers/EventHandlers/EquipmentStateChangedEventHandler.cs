using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using EAP.Gateway.Core.Events.Equipment;
using EAP.Gateway.Core.Models;
using EAP.Gateway.Core.Repositories;
using EAP.Gateway.Core.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EAP.Gateway.Application.Handlers.EventHandlers;

/// <summary>
/// 设备状态变更事件处理器（最终修复版本）
/// 处理 EquipmentStatusChangedEvent 事件
/// </summary>
public class EquipmentStateChangedEventHandler : INotificationHandler<EquipmentStatusChangedEvent>
{
    private readonly IDeviceStatusCacheService _cacheService;
    private readonly IKafkaProducerService _kafkaProducer;
    private readonly ILogger<EquipmentStateChangedEventHandler> _logger;

    public EquipmentStateChangedEventHandler(
        IDeviceStatusCacheService cacheService,
        IKafkaProducerService kafkaProducer,
        ILogger<EquipmentStateChangedEventHandler> logger)
    {
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _kafkaProducer = kafkaProducer ?? throw new ArgumentNullException(nameof(kafkaProducer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 处理设备状态变更事件（修复版本）
    /// </summary>
    public async Task Handle(EquipmentStatusChangedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("处理设备状态变更事件: {EquipmentId}, {PreviousState} -> {NewState}, 变更类型: {ChangeType}, 变更者: {ChangedBy}",
                notification.EquipmentId.Value,
                notification.PreviousState,
                notification.NewState,
                notification.ChangeType,
                notification.ChangedBy ?? "System");

            // 更新缓存中的设备状态
            await UpdateDeviceStatusCacheAsync(notification, cancellationToken);

            // 发布到Kafka供外部系统消费
            await PublishStateChangeEventAsync(notification, cancellationToken);

            // 记录严重状态变化
            if (notification.IsCriticalChange)
            {
                _logger.LogWarning("检测到严重状态变化: {EquipmentId}, {PreviousState} -> {NewState}, 原因: {Reason}",
                    notification.EquipmentId.Value,
                    notification.PreviousState,
                    notification.NewState,
                    notification.Reason ?? "未指定");
            }

            _logger.LogInformation("设备状态变更事件处理完成: {EquipmentId}", notification.EquipmentId.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理设备状态变更事件失败: {EquipmentId}, 错误: {ErrorMessage}",
                notification.EquipmentId.Value, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// 更新设备状态缓存
    /// </summary>
    private async Task UpdateDeviceStatusCacheAsync(EquipmentStatusChangedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var currentStatus = await _cacheService.GetEquipmentStatusAsync(notification.EquipmentId, cancellationToken);

            if (currentStatus != null)
            {
                // 使用正确的不可变更新方法
                var updatedStatus = currentStatus
                    .WithState(notification.NewState)     // 使用枚举版本
                    .WithUpdatedAt(notification.ChangedAt);

                // 如果有子状态信息，也更新
                if (!string.IsNullOrEmpty(notification.SubState))
                {
                    updatedStatus = updatedStatus.WithSubState(notification.SubState);
                }

                // 根据新状态更新健康状态
                var newHealthStatus = DetermineHealthStatusFromState(notification.NewState, currentStatus.ActiveAlarmsCount);
                if (newHealthStatus != currentStatus.HealthStatus)
                {
                    updatedStatus = updatedStatus.WithHealthStatus(newHealthStatus);
                }

                await _cacheService.SetEquipmentStatusAsync(updatedStatus, cancellationToken);

                _logger.LogDebug("已更新设备状态缓存: {EquipmentId}, 新状态: {NewState}",
                    notification.EquipmentId.Value, notification.NewState);
            }
            else
            {
                _logger.LogWarning("设备状态缓存不存在，无法更新: {EquipmentId}", notification.EquipmentId.Value);

                // 可选：创建新的设备状态缓存条目
                await CreateNewEquipmentStatusCache(notification, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新设备状态缓存失败: {EquipmentId}", notification.EquipmentId.Value);
            throw;
        }
    }

    /// <summary>
    /// 根据设备状态确定健康状态
    /// </summary>
    private static HealthStatus DetermineHealthStatusFromState(EquipmentState state, int activeAlarmsCount)
    {
        return state switch
        {
            EquipmentState.FAULT => HealthStatus.Unhealthy,
            EquipmentState.ALARM when activeAlarmsCount > 0 => HealthStatus.Degraded,
            EquipmentState.DOWN => HealthStatus.Unhealthy,
            EquipmentState.MAINTENANCE => HealthStatus.Degraded,
            EquipmentState.IDLE or EquipmentState.EXECUTING or EquipmentState.SETUP => HealthStatus.Healthy,
            EquipmentState.PAUSE => HealthStatus.Healthy,
            _ => HealthStatus.Unknown
        };
    }

    /// <summary>
    /// 创建新的设备状态缓存条目
    /// </summary>
    private async Task CreateNewEquipmentStatusCache(EquipmentStatusChangedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            // 创建基础的设备状态
            var connectionState = Core.ValueObjects.ConnectionState.Disconnected();
            var healthStatus = DetermineHealthStatusFromState(notification.NewState, 0);

            var newStatus = EquipmentStatus.Create(
                notification.EquipmentId,
                $"Device_{notification.EquipmentId.Value}", // 使用默认名称，实际应该从设备仓储获取
                notification.NewState,
                connectionState,
                healthStatus);

            // 如果有子状态，更新
            if (!string.IsNullOrEmpty(notification.SubState))
            {
                newStatus = newStatus.WithSubState(notification.SubState);
            }

            await _cacheService.SetEquipmentStatusAsync(newStatus, cancellationToken);

            _logger.LogInformation("创建新的设备状态缓存: {EquipmentId}", notification.EquipmentId.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建设备状态缓存失败: {EquipmentId}", notification.EquipmentId.Value);
        }
    }


    /// <summary>
    /// 发布状态变更事件到Kafka
    /// </summary>
    private async Task PublishStateChangeEventAsync(EquipmentStatusChangedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var eventData = new
            {
                EquipmentId = notification.EquipmentId.Value,
                PreviousState = notification.PreviousState.ToString(),
                NewState = notification.NewState.ToString(),
                Reason = notification.Reason,
                ChangedAt = notification.ChangedAt,
                ChangedBy = notification.ChangedBy,
                ChangeType = notification.ChangeType.ToString(),
                SubState = notification.SubState,
                IsAutomaticChange = notification.IsAutomaticChange,
                IsCriticalChange = notification.IsCriticalChange,
                IsRecoveryChange = notification.IsRecoveryChange,
                SeverityLevel = notification.GetSeverityLevel(),
                PreviousStateDuration = notification.PreviousStateDuration?.TotalSeconds,
                Context = notification.Context,
                EventId = notification.EventId,
                OccurredOn = notification.OccurredOn
            };

            // 根据状态变更的严重程度选择不同的Kafka主题
            var topicName = notification.IsCriticalChange ?
                "eap.equipment.critical_state_changes" :
                "eap.equipment.state_changes";

            await _kafkaProducer.PublishEquipmentEventAsync(
                notification.EquipmentId.Value,
                "EquipmentStatusChanged",
                eventData,
                cancellationToken);

            _logger.LogDebug("已发布设备状态变更事件到Kafka: {EquipmentId}, Topic: {Topic}, EventId: {EventId}",
                notification.EquipmentId.Value, topicName, notification.EventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发布设备状态变更事件到Kafka失败: {EquipmentId}", notification.EquipmentId.Value);
            throw;
        }
    }
}
