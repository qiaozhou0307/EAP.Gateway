using MediatR;
using Microsoft.Extensions.Logging;
using EAP.Gateway.Core.Events.Data;
using EAP.Gateway.Core.Repositories;

namespace EAP.Gateway.Application.Handlers.EventHandlers;

/// <summary>
/// 追踪数据接收事件处理器
/// 支持FR-BLL-005需求：实时数据处理(RTM/FMB)
/// </summary>
public class TraceDataReceivedEventHandler : INotificationHandler<TraceDataReceivedEvent>
{
    private readonly IDeviceStatusCacheService _cacheService;
    private readonly IKafkaProducerService _kafkaProducer;
    private readonly ILogger<TraceDataReceivedEventHandler> _logger;

    public TraceDataReceivedEventHandler(
        IDeviceStatusCacheService cacheService,
        IKafkaProducerService kafkaProducer,
        ILogger<TraceDataReceivedEventHandler> logger)
    {
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _kafkaProducer = kafkaProducer ?? throw new ArgumentNullException(nameof(kafkaProducer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Handle(TraceDataReceivedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("处理追踪数据事件: {EquipmentId}, 数据点数: {Count}",
                notification.EquipmentId.Value, notification.DataVariables.Count);

            // 批量更新缓存中的数据变量
            await _cacheService.UpdateDataVariablesAsync(
                notification.EquipmentId,
                notification.DataVariables,
                cancellationToken);

            // 更新设备状态的最后数据更新时间
            var currentStatus = await _cacheService.GetEquipmentStatusAsync(notification.EquipmentId, cancellationToken);
            if (currentStatus != null)
            {
                var updatedStatus = currentStatus.WithLastDataUpdate(notification.ReceivedAt);
                await _cacheService.SetEquipmentStatusAsync(updatedStatus, cancellationToken);
            }

            // 发布到Kafka供其他系统消费
            var traceDataDto = new
            {
                EquipmentId = notification.EquipmentId.Value,
                DataVariables = notification.DataVariables.ToDictionary(
                    kvp => kvp.Key.ToString(),
                    kvp => kvp.Value),
                ReceivedAt = notification.ReceivedAt,
                LotId = notification.LotId,
                CarrierId = notification.CarrierId
            };

            await _kafkaProducer.PublishTraceDataAsync(
                "eap.trace_data",
                $"{notification.EquipmentId.Value}:{DateTime.UtcNow.Ticks}",
                traceDataDto,
                cancellationToken);

            _logger.LogDebug("追踪数据事件处理完成: {EquipmentId}", notification.EquipmentId.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理追踪数据事件失败: {EquipmentId}", notification.EquipmentId.Value);
        }
    }
}
