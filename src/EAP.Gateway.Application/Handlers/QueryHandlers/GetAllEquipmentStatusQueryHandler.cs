using EAP.Gateway.Application.DTOs;
using EAP.Gateway.Application.Extensions;
using EAP.Gateway.Application.Queries.Equipment;
using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using EAP.Gateway.Core.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EAP.Gateway.Application.Handlers.QueryHandlers;

/// <summary>
/// 获取所有设备状态查询处理器
/// 修复了DTO属性匹配和方法调用问题
/// </summary>
public class GetAllEquipmentStatusQueryHandler : IRequestHandler<GetAllEquipmentStatusQuery, IEnumerable<EquipmentStatusDto>>
{
    private readonly IDeviceStatusCacheService _cacheService;
    private readonly IEquipmentRepository _equipmentRepository;
    private readonly ILogger<GetAllEquipmentStatusQueryHandler> _logger;

    public GetAllEquipmentStatusQueryHandler(
        IDeviceStatusCacheService cacheService,
        IEquipmentRepository equipmentRepository,
        ILogger<GetAllEquipmentStatusQueryHandler> logger)
    {
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _equipmentRepository = equipmentRepository ?? throw new ArgumentNullException(nameof(equipmentRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<EquipmentStatusDto>> Handle(GetAllEquipmentStatusQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // 首先尝试从缓存获取所有设备状态
            var allStatusFromCache = await _cacheService.GetAllEquipmentStatusAsync(cancellationToken);

            if (allStatusFromCache?.Any() == true)
            {
                _logger.LogDebug("从缓存获取到 {Count} 个设备状态", allStatusFromCache.Count());

                var results = allStatusFromCache.Select(status => status.ToDto()).ToList();

                // 应用过滤器 - 修复：使用ConnectionState属性而不是IsConnected
                if (!request.IncludeDisconnected)
                {
                    results = results.Where(s => s.ConnectionState == "Connected").ToList();
                }

                if (request.MaxResults.HasValue)
                {
                    results = results.Take(request.MaxResults.Value).ToList();
                }

                return results;
            }

            // 如果缓存中没有数据，从数据库获取基本设备列表
            _logger.LogWarning("缓存中没有设备状态数据，从数据库获取设备列表");

            var equipmentList = await _equipmentRepository.GetAllAsync(cancellationToken);
            var statusDtos = new List<EquipmentStatusDto>();

            foreach (var equipment in equipmentList)
            {
                // 使用映射扩展方法创建状态DTO - 更简洁且类型安全
                var statusDto = equipment.ToStatusDto();
                statusDtos.Add(statusDto);
            }

            return statusDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取所有设备状态时发生异常");
            return Enumerable.Empty<EquipmentStatusDto>();
        }
    }
}
