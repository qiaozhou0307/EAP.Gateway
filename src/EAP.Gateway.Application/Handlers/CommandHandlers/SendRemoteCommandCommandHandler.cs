using EAP.Gateway.Application.Commands.Equipment;
using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using EAP.Gateway.Core.Repositories;
using EAP.Gateway.Core.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EAP.Gateway.Application.Handlers.CommandHandlers;

/// <summary>
/// 发送远程命令处理器 - 直接修复版本
/// 基于现有Equipment实体方法，最小化修改
/// </summary>
public class SendRemoteCommandCommandHandler : IRequestHandler<SendRemoteCommandCommand, SendRemoteCommandResult>
{
    private readonly IEquipmentRepository _equipmentRepository;
    private readonly ISecsDeviceManager _deviceManager;
    private readonly ILogger<SendRemoteCommandCommandHandler> _logger;

    public SendRemoteCommandCommandHandler(
        IEquipmentRepository equipmentRepository,
        ISecsDeviceManager deviceManager,
        ILogger<SendRemoteCommandCommandHandler> logger)
    {
        _equipmentRepository = equipmentRepository ?? throw new ArgumentNullException(nameof(equipmentRepository));
        _deviceManager = deviceManager ?? throw new ArgumentNullException(nameof(deviceManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<SendRemoteCommandResult> Handle(SendRemoteCommandCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("开始发送远程命令 - 设备: {EquipmentId}, 命令: {Command}",
                request.EquipmentId.Value, request.CommandName);

            // 获取设备实体
            var equipment = await _equipmentRepository.GetByIdAsync(request.EquipmentId, cancellationToken);
            if (equipment == null)
            {
                return new SendRemoteCommandResult(
                    false,
                    "设备不存在",
                    Guid.Empty,
                    DateTime.UtcNow,
                    $"设备ID {request.EquipmentId.Value} 不存在");
            }

            // 检查设备是否可以执行命令
            if (!equipment.CanExecuteCommand(request.CommandName))
            {
                return new SendRemoteCommandResult(
                    false,
                    "设备当前状态不允许执行此命令",
                    Guid.Empty,
                    DateTime.UtcNow,
                    $"设备状态: {equipment.State}, 连接状态: {equipment.ConnectionState?.IsConnected}");
            }

            // 修复：使用ExecuteRemoteCommand方法（这个方法确实存在）
            var commandId = equipment.ExecuteRemoteCommand(
                request.CommandName,
                request.Parameters,
                request.RequestedBy,
                request.TimeoutSeconds);

            // 保存设备状态变更
            await _equipmentRepository.UpdateAsync(equipment, cancellationToken);

            // 通过设备管理器实际发送命令到设备
            var deviceService = await _deviceManager.GetDeviceServiceAsync(request.EquipmentId, cancellationToken);
            if (deviceService != null)
            {
                // 修复：根据ISecsDeviceService接口定义调用方法
                var commandResult = await deviceService.SendRemoteCommandAsync(
                    request.CommandName,
                    request.Parameters,
                    request.RequestedBy,
                    cancellationToken);

                if (!commandResult.IsSuccessful)
                {
                    // 如果发送失败，更新命令状态
                    equipment.UpdateCommandStatus(commandId, CommandStatus.Failed, commandResult.ErrorMessage ?? "设备通信失败");
                    await _equipmentRepository.UpdateAsync(equipment, cancellationToken);

                    return new SendRemoteCommandResult(
                        false,
                        "命令发送到设备失败",
                        commandId,
                        DateTime.UtcNow,
                        commandResult.ErrorMessage ?? "设备通信失败");
                }

                // 更新命令状态为已完成
                equipment.UpdateCommandStatus(commandId, CommandStatus.Completed, commandResult.ResultMessage);
                await _equipmentRepository.UpdateAsync(equipment, cancellationToken);
            }

            _logger.LogInformation("远程命令发送成功 - 设备: {EquipmentId}, 命令: {Command}, CommandId: {CommandId}",
                request.EquipmentId.Value, request.CommandName, commandId);

            return new SendRemoteCommandResult(
                true,
                "命令发送成功",
                commandId,
                DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送远程命令时发生异常 - 设备: {EquipmentId}, 命令: {Command}",
                request.EquipmentId.Value, request.CommandName);

            return new SendRemoteCommandResult(
                false,
                "发送命令时发生内部错误",
                Guid.Empty,
                DateTime.UtcNow,
                ex.Message);
        }
    }
}
