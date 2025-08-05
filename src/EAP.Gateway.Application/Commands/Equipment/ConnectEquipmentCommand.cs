using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using EAP.Gateway.Core.ValueObjects;
using MediatR;

namespace EAP.Gateway.Application.Commands.Equipment;

/// <summary>
/// 连接设备命令
/// 支持FR-ECM-001需求：HSMS连接建立与维护
/// </summary>
public record ConnectEquipmentCommand(
    EquipmentId EquipmentId,
    bool ForceReconnect = false
) : IRequest<ConnectEquipmentResult>;

public record ConnectEquipmentResult(
    bool IsSuccessful,
    string Message,
    DateTime? ConnectedAt = null,
    string? SessionId = null
);
