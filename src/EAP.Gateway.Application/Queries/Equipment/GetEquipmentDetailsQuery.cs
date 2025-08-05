using EAP.Gateway.Application.DTOs;
using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using EAP.Gateway.Core.ValueObjects;
using MediatR;

namespace EAP.Gateway.Application.Queries.Equipment;

/// <summary>
/// 获取设备详细信息查询
/// 支持FR-API-001需求：设备详细信息查询API
/// </summary>
public record GetEquipmentDetailsQuery(
    EquipmentId EquipmentId,
    bool IncludeConfiguration = true,
    bool IncludeMetrics = true,
    bool IncludeAlarms = true,
    bool IncludeRecentCommands = true
) : IRequest<EquipmentDetailsDto?>;
