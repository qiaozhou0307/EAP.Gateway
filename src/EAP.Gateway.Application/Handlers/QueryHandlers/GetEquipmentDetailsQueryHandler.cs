using EAP.Gateway.Application.DTOs;
using EAP.Gateway.Application.Extensions;
using EAP.Gateway.Application.Queries.Equipment;
using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using EAP.Gateway.Core.Repositories;
using EAP.Gateway.Core.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EAP.Gateway.Application.Handlers.QueryHandlers;

/// <summary>
/// 获取设备详细信息查询处理器
/// 使用Equipment实体的直接属性，无需从Configuration中获取不存在的属性
/// </summary>
public class GetEquipmentDetailsQueryHandler : IRequestHandler<GetEquipmentDetailsQuery, EquipmentDetailsDto?>
{
    private readonly IEquipmentRepository _equipmentRepository;
    private readonly IDeviceStatusCacheService _cacheService;
    private readonly ILogger<GetEquipmentDetailsQueryHandler> _logger;

    public GetEquipmentDetailsQueryHandler(
        IEquipmentRepository equipmentRepository,
        IDeviceStatusCacheService cacheService,
        ILogger<GetEquipmentDetailsQueryHandler> logger)
    {
        _equipmentRepository = equipmentRepository ?? throw new ArgumentNullException(nameof(equipmentRepository));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<EquipmentDetailsDto?> Handle(GetEquipmentDetailsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("获取设备详细信息: {EquipmentId}", request.EquipmentId.Value);

            // 从数据库获取设备实体
            var equipment = await _equipmentRepository.GetByIdAsync(request.EquipmentId, cancellationToken);
            if (equipment == null)
            {
                _logger.LogWarning("设备未找到: {EquipmentId}", request.EquipmentId.Value);
                return null;
            }

            // 使用映射扩展方法创建详细信息DTO - 更简洁且类型安全
            var detailsDto = equipment.ToDetailsDto(
                includeConfiguration: request.IncludeConfiguration,
                includeMetrics: request.IncludeMetrics,
                includeAlarms: request.IncludeAlarms,
                includeCommands: request.IncludeRecentCommands
            );
            return detailsDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取设备详细信息时发生异常: {EquipmentId}", request.EquipmentId.Value);
            return null;
        }
    }

}
