using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using EAP.Gateway.Core.ValueObjects;
using MediatR;

namespace EAP.Gateway.Application.Commands.Equipment;

/// <summary>
/// 发送远程命令命令
/// 支持FR-RCC-001需求：远程命令控制
/// </summary>
public record SendRemoteCommandCommand(
    EquipmentId EquipmentId,
    string CommandName,
    Dictionary<string, object>? Parameters = null,
    string? RequestedBy = null,
    int TimeoutSeconds = 30
) : IRequest<SendRemoteCommandResult>;

/// <summary>
/// 发送远程命令结果
/// </summary>
public record SendRemoteCommandResult(
    bool IsSuccessful,
    string Message,
    Guid CommandId,
    DateTime RequestedAt,
    string? ErrorDetails = null
);
