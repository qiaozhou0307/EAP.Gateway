using EAP.Gateway.Application.DTOs;
using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using EAP.Gateway.Core.ValueObjects;
using MediatR;

namespace EAP.Gateway.Application.Queries.Data;

/// <summary>
/// 获取数据变量查询
/// 支持FR-API-002需求：数据变量/设备常数查询API
/// </summary>
public record GetDataVariablesQuery(
    EquipmentId EquipmentId,
    uint[]? VariableIds = null
) : IRequest<DataVariablesDto>;
