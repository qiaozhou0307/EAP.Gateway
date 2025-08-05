using EAP.Gateway.Application.DTOs;
using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using EAP.Gateway.Core.ValueObjects;
using MediatR;

namespace EAP.Gateway.Application.Queries.Equipment;

/// <summary>
/// 获取设备状态查询
/// 支持FR-API-001需求：设备状态查询API
/// </summary>
public record GetEquipmentStatusQuery(
    EquipmentId EquipmentId
) : IRequest<EquipmentStatusDto?>;
