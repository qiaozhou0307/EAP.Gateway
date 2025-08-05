using EAP.Gateway.Application.Commands.Equipment;
using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using EAP.Gateway.Core.Repositories; // 现在引用Core层的接口
using MediatR;
using Microsoft.Extensions.Logging;

namespace EAP.Gateway.Application.Handlers.CommandHandlers;

/// <summary>
/// 连接设备命令处理器
/// </summary>
public class ConnectEquipmentCommandHandler : IRequestHandler<ConnectEquipmentCommand, ConnectEquipmentResult>
{
    private readonly IEquipmentRepository _equipmentRepository;
    private readonly ISecsDeviceManager _deviceManager; // 现在引用Core层的接口
    private readonly ILogger<ConnectEquipmentCommandHandler> _logger;

    public ConnectEquipmentCommandHandler(
        IEquipmentRepository equipmentRepository,
        ISecsDeviceManager deviceManager, // 依赖注入会提供Infrastructure层的实现
        ILogger<ConnectEquipmentCommandHandler> logger)
    {
        _equipmentRepository = equipmentRepository;
        _deviceManager = deviceManager;
        _logger = logger;
    }

    public async Task<ConnectEquipmentResult> Handle(ConnectEquipmentCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("开始连接设备 {EquipmentId}", request.EquipmentId.Value);

            var equipment = await _equipmentRepository.GetByIdAsync(request.EquipmentId, cancellationToken);
            if (equipment == null)
            {
                return new ConnectEquipmentResult(false, "设备不存在");
            }

            var deviceService = await _deviceManager.GetDeviceServiceAsync(request.EquipmentId, cancellationToken);
            if (deviceService == null)
            {
                return new ConnectEquipmentResult(false, "设备服务未初始化");
            }

            var isConnected = await deviceService.ConnectAsync(cancellationToken);
            if (isConnected)
            {
                var connectedAt = DateTime.UtcNow;
                _logger.LogInformation("设备 {EquipmentId} 连接成功", request.EquipmentId.Value);

                return new ConnectEquipmentResult(true, "连接成功", connectedAt);
            }
            else
            {
                _logger.LogWarning("设备 {EquipmentId} 连接失败", request.EquipmentId.Value);
                return new ConnectEquipmentResult(false, "连接失败");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "连接设备 {EquipmentId} 时发生异常", request.EquipmentId.Value);
            return new ConnectEquipmentResult(false, $"连接异常: {ex.Message}");
        }
    }
}
