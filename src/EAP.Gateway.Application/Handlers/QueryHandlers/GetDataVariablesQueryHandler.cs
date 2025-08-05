// 文件路径: src/EAP.Gateway.Application/Handlers/QueryHandlers/GetDataVariablesQueryHandler.cs
using EAP.Gateway.Application.DTOs;
using EAP.Gateway.Application.Extensions;
using EAP.Gateway.Application.Queries.Data;
using EAP.Gateway.Core.Repositories;
using EAP.Gateway.Core.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EAP.Gateway.Application.Handlers.QueryHandlers;

/// <summary>
/// 获取数据变量查询处理器（修正版本）
/// 支持FR-API-002需求：数据变量/设备常数查询API
/// </summary>
public class GetDataVariablesQueryHandler : IRequestHandler<GetDataVariablesQuery, DataVariablesDto>
{
    private readonly IDeviceStatusCacheService _cacheService;
    private readonly ILogger<GetDataVariablesQueryHandler> _logger;

    public GetDataVariablesQueryHandler(
        IDeviceStatusCacheService cacheService,
        ILogger<GetDataVariablesQueryHandler> logger)
    {
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<DataVariablesDto> Handle(GetDataVariablesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("开始处理获取数据变量请求 {EquipmentId}, 变量ID: {VariableIds}",
                request.EquipmentId.Value,
                request.VariableIds != null ? string.Join(",", request.VariableIds) : "全部");

            // 从缓存获取数据变量
            var dataVariables = await _cacheService.GetDataVariablesAsync(request.EquipmentId, cancellationToken);

            if (dataVariables == null)
            {
                _logger.LogWarning("数据变量缓存未找到 {EquipmentId}", request.EquipmentId.Value);
                return DataVariablesDto.Empty(request.EquipmentId.Value);
            }

            // 记录找到的变量数量
            _logger.LogDebug("找到数据变量 {Count} 个，设备 {EquipmentId}",
                dataVariables.Variables.Count,
                request.EquipmentId.Value);

            // 如果指定了特定的变量ID，则进行过滤
            if (request.VariableIds?.Length > 0)
            {
                return HandleFilteredRequest(dataVariables, request.VariableIds);
            }

            // 转换完整的数据变量集合 - 使用扩展方法
            return dataVariables.ToDto();
        }
        catch (ArgumentException argEx)
        {
            _logger.LogError(argEx, "获取数据变量时参数异常 {EquipmentId}", request.EquipmentId.Value);
            return DataVariablesDto.Empty(request.EquipmentId.Value);
        }
        catch (InvalidOperationException opEx)
        {
            _logger.LogError(opEx, "获取数据变量时操作异常 {EquipmentId}", request.EquipmentId.Value);
            return DataVariablesDto.Empty(request.EquipmentId.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取数据变量时发生未知异常 {EquipmentId}", request.EquipmentId.Value);
            return DataVariablesDto.Empty(request.EquipmentId.Value);
        }
    }

    /// <summary>
    /// 处理过滤请求（同步方法）
    /// </summary>
    private DataVariablesDto HandleFilteredRequest(DataVariables dataVariables, uint[] requestedVariableIds)
    {
        var requestedIds = new HashSet<uint>(requestedVariableIds);

        // 性能优化：大数据集使用并行查询
        var filteredVariables = dataVariables.Variables.Count > 100
            ? dataVariables.Variables.AsParallel()
                .Where(kvp => requestedIds.Contains(kvp.Key))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            : dataVariables.Variables
                .Where(kvp => requestedIds.Contains(kvp.Key))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        // 记录过滤结果
        var foundIds = filteredVariables.Keys.ToList();
        var missingIds = requestedIds.Except(foundIds).ToList();

        if (missingIds.Any())
        {
            _logger.LogWarning("请求的变量ID中有部分未找到 {MissingIds}, 设备 {EquipmentId}",
                string.Join(",", missingIds),
                dataVariables.EquipmentId.Value);
        }

        _logger.LogDebug("过滤后的数据变量 {FilteredCount}/{RequestedCount} 个",
            filteredVariables.Count,
            requestedVariableIds.Length);

        // 创建过滤后的领域模型并转换为DTO
        var filteredDataVariables = DataVariables.Create(
            dataVariables.EquipmentId,
            filteredVariables,
            dataVariables.LastUpdated);

        // 使用扩展方法转换
        return filteredDataVariables.ToDto();
    }
}
