using MediatR;
using Microsoft.Extensions.Logging;
using EAP.Gateway.Application.Queries.Equipment;
using EAP.Gateway.Application.DTOs;
using EAP.Gateway.Application.Extensions;
using EAP.Gateway.Core.Repositories;

namespace EAP.Gateway.Application.Handlers.QueryHandlers;

/// <summary>
/// 获取设备状态查询处理器
/// 支持FR-API-001需求：设备状态查询API (从Redis获取设备状态)
/// </summary>
public class GetEquipmentStatusQueryHandler : IRequestHandler<GetEquipmentStatusQuery, EquipmentStatusDto?>
{
    private readonly IDeviceStatusCacheService _cacheService;
    private readonly ILogger<GetEquipmentStatusQueryHandler> _logger;

    public GetEquipmentStatusQueryHandler(
        IDeviceStatusCacheService cacheService,
        ILogger<GetEquipmentStatusQueryHandler> logger)
    {
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<EquipmentStatusDto?> Handle(GetEquipmentStatusQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var equipmentStatus = await _cacheService.GetEquipmentStatusAsync(request.EquipmentId, cancellationToken);

            if (equipmentStatus != null)
            {
                _logger.LogDebug("从缓存获取设备状态 {EquipmentId}", request.EquipmentId.Value);
                return equipmentStatus.ToDto(); // ✅ 使用映射扩展方法
            }

            _logger.LogWarning("设备状态缓存未找到 {EquipmentId}", request.EquipmentId.Value);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取设备状态时发生异常 {EquipmentId}", request.EquipmentId.Value);
            return null;
        }
    }
}
