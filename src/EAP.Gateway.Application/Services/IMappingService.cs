using EAP.Gateway.Application.DTOs;
using EAP.Gateway.Core.ValueObjects;

namespace EAP.Gateway.Application.Services;

/// <summary>
/// 映射服务接口 - 提供更好的可测试性和扩展性
/// </summary>
public interface IMappingService
{
    /// <summary>
    /// 将DataVariables领域模型映射为DTO
    /// </summary>
    DataVariablesDto MapToDto(DataVariables dataVariables);

    /// <summary>
    /// 将DataVariable领域模型映射为DTO
    /// </summary>
    DataVariableValueDto MapToDto(DataVariable dataVariable);

    /// <summary>
    /// 批量映射DataVariables
    /// </summary>
    IEnumerable<DataVariablesDto> MapToDto(IEnumerable<DataVariables> dataVariablesList);
}
