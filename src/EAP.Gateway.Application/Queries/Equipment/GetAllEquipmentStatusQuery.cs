using EAP.Gateway.Application.DTOs;
using MediatR;

namespace EAP.Gateway.Application.Queries.Equipment;

/// <summary>
/// 获取所有设备状态查询
/// 支持FR-API-001需求：获取所有设备状态列表
/// </summary>
public record GetAllEquipmentStatusQuery(
    bool IncludeDisconnected = true,
    bool IncludeAlarmInfo = true,
    int? MaxResults = null
) : IRequest<IEnumerable<EquipmentStatusDto>>;
