using EAP.Gateway.Application.DTOs;
using EAP.Gateway.Core.ValueObjects;
using Microsoft.Extensions.Logging;

namespace EAP.Gateway.Application.Services;

/// <summary>
/// 映射服务实现
/// </summary>
public class MappingService : IMappingService
{
    private readonly ILogger<MappingService> _logger;

    public MappingService(ILogger<MappingService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public DataVariablesDto MapToDto(DataVariables dataVariables)
    {
        ArgumentNullException.ThrowIfNull(dataVariables);

        try
        {
            var variablesDict = new Dictionary<uint, DataVariableValueDto>();

            // 批量转换优化
            foreach (var (id, variable) in dataVariables.Variables)
            {
                variablesDict[id] = MapToDto(variable);
            }

            return new DataVariablesDto
            {
                EquipmentId = dataVariables.EquipmentId.Value,
                Variables = variablesDict,
                LastUpdated = dataVariables.LastUpdated
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "映射DataVariables到DTO失败 {EquipmentId}", dataVariables.EquipmentId.Value);
            throw new InvalidOperationException($"Failed to map DataVariables for equipment {dataVariables.EquipmentId.Value}", ex);
        }
    }

    public DataVariableValueDto MapToDto(DataVariable dataVariable)
    {
        ArgumentNullException.ThrowIfNull(dataVariable);

        return new DataVariableValueDto
        {
            Id = dataVariable.Id,
            Name = dataVariable.Name,
            Value = dataVariable.Value,
            DataType = dataVariable.DataType,
            Unit = dataVariable.Unit,
            Quality = dataVariable.Quality,
            Timestamp = dataVariable.Timestamp
        };
    }

    public IEnumerable<DataVariablesDto> MapToDto(IEnumerable<DataVariables> dataVariablesList)
    {
        ArgumentNullException.ThrowIfNull(dataVariablesList);

        return dataVariablesList.Select(MapToDto);
    }
}
