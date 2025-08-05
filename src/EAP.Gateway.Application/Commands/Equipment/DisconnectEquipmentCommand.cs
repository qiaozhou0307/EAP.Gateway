using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using EAP.Gateway.Core.ValueObjects;
using MediatR;

namespace EAP.Gateway.Application.Commands.Equipment;

/// <summary>
/// 断开设备连接命令
/// </summary>
public record DisconnectEquipmentCommand(
    EquipmentId EquipmentId,
    string Reason = "Manual disconnect"
) : IRequest<DisconnectEquipmentResult>;

public record DisconnectEquipmentResult(
    bool IsSuccessful,
    string Message,
    DateTime? DisconnectedAt = null
);
