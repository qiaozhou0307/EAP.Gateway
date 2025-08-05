using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using EAP.Gateway.Core.ValueObjects;
using EAP.Gateway.Core.Common;
using EAP.Gateway.Infrastructure.Persistence.Contexts;

namespace EAP.Gateway.Infrastructure.Persistence.Repositories;

/// <summary>
/// 设备仓储实现
/// </summary>
public class EquipmentRepository : IEquipmentRepository
{
    private readonly EapGatewayDbContext _context;
    private readonly ILogger<EquipmentRepository> _logger;

    public EquipmentRepository(EapGatewayDbContext context, ILogger<EquipmentRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Equipment?> GetByIdAsync(EquipmentId equipmentId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Equipment
                .FirstOrDefaultAsync(e => e.Id == equipmentId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取设备失败: {EquipmentId}", equipmentId.Value);
            throw;
        }
    }

    public async Task<Equipment> GetByIdRequiredAsync(EquipmentId equipmentId, CancellationToken cancellationToken = default)
    {
        var equipment = await GetByIdAsync(equipmentId, cancellationToken);
        return equipment ?? throw new EquipmentNotFoundException(equipmentId);
    }

    public async Task<Equipment?> GetByEndpointAsync(IpEndpoint endpoint, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Equipment
                .FirstOrDefaultAsync(e => e.Configuration.Endpoint.IpAddress == endpoint.IpAddress &&
                                         e.Configuration.Endpoint.Port == endpoint.Port, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据端点获取设备失败: {Endpoint}", endpoint);
            throw;
        }
    }

    public async Task<IReadOnlyList<Equipment>> GetByStateAsync(EquipmentState state, CancellationToken cancellationToken = default)
    {
        try
        {
            var equipment = await _context.Equipment
                .Where(e => e.State == state)
                .ToListAsync(cancellationToken);
            return equipment.AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据状态获取设备失败: {State}", state);
            throw;
        }
    }

    public async Task<IReadOnlyList<Equipment>> GetByConnectionStateAsync(bool isConnected, CancellationToken cancellationToken = default)
    {
        try
        {
            var equipment = await _context.Equipment
                .Where(e => e.ConnectionState.IsConnected == isConnected)
                .ToListAsync(cancellationToken);
            return equipment.AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据连接状态获取设备失败: {IsConnected}", isConnected);
            throw;
        }
    }

    public async Task<IReadOnlyList<Equipment>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var equipment = await _context.Equipment.ToListAsync(cancellationToken);
            return equipment.AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取所有设备失败");
            throw;
        }
    }

    public async Task<PagedResult<Equipment>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        try
        {
            var totalCount = await _context.Equipment.CountAsync(cancellationToken);
            var equipment = await _context.Equipment
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<Equipment>(equipment.AsReadOnly(), totalCount, pageNumber, pageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "分页获取设备失败: Page={PageNumber}, Size={PageSize}", pageNumber, pageSize);
            throw;
        }
    }

    public async Task<IReadOnlyList<Equipment>> GetByFilterAsync(EquipmentFilter filter, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.Equipment.AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.NameFilter))
            {
                query = query.Where(e => e.Name.Contains(filter.NameFilter));
            }

            if (filter.State.HasValue)
            {
                query = query.Where(e => e.State == filter.State.Value);
            }

            if (filter.IsConnected.HasValue)
            {
                query = query.Where(e => e.ConnectionState.IsConnected == filter.IsConnected.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.IpAddress))
            {
                query = query.Where(e => e.Configuration.Endpoint.IpAddress == filter.IpAddress);
            }

            if (filter.LastUpdatedFrom.HasValue)
            {
                query = query.Where(e => e.UpdatedAt >= filter.LastUpdatedFrom.Value);
            }

            if (filter.LastUpdatedTo.HasValue)
            {
                query = query.Where(e => e.UpdatedAt <= filter.LastUpdatedTo.Value);
            }

            // 排序
            if (!string.IsNullOrWhiteSpace(filter.SortBy))
            {
                query = filter.SortDirection == SortDirection.Ascending
                    ? query.OrderBy(e => EF.Property<object>(e, filter.SortBy))
                    : query.OrderByDescending(e => EF.Property<object>(e, filter.SortBy));
            }
            else
            {
                query = query.OrderBy(e => e.Name);
            }

            var equipment = await query.ToListAsync(cancellationToken);
            return equipment.AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据过滤条件获取设备失败");
            throw;
        }
    }

    public async Task<bool> ExistsAsync(EquipmentId equipmentId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Equipment.AnyAsync(e => e.Id == equipmentId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查设备是否存在失败: {EquipmentId}", equipmentId.Value);
            throw;
        }
    }

    public async Task<Equipment> AddAsync(Equipment equipment, CancellationToken cancellationToken = default)
    {
        try
        {
            _context.Equipment.Add(equipment);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("添加设备成功: {EquipmentId}", equipment.Id.Value);
            return equipment;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加设备失败: {EquipmentId}", equipment.Id.Value);
            throw;
        }
    }

    public async Task<Equipment> UpdateAsync(Equipment equipment, CancellationToken cancellationToken = default)
    {
        try
        {
            _context.Equipment.Update(equipment);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("更新设备成功: {EquipmentId}", equipment.Id.Value);
            return equipment;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新设备失败: {EquipmentId}", equipment.Id.Value);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(EquipmentId equipmentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var equipment = await GetByIdAsync(equipmentId, cancellationToken);
            if (equipment == null)
            {
                return false;
            }

            _context.Equipment.Remove(equipment);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("删除设备成功: {EquipmentId}", equipmentId.Value);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除设备失败: {EquipmentId}", equipmentId.Value);
            throw;
        }
    }

    public async Task<int> BatchUpdateStateAsync(IEnumerable<EquipmentStateUpdate> updates, CancellationToken cancellationToken = default)
    {
        try
        {
            var count = 0;
            foreach (var update in updates)
            {
                var equipment = await GetByIdAsync(update.EquipmentId, cancellationToken);
                if (equipment != null)
                {
                    equipment.UpdateState(update.NewState, update.Reason);
                    _context.Equipment.Update(equipment);
                    count++;
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("批量更新设备状态成功，影响 {Count} 台设备", count);
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量更新设备状态失败");
            throw;
        }
    }

    public async Task<EquipmentStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var totalCount = await _context.Equipment.CountAsync(cancellationToken);
            var connectedCount = await _context.Equipment.CountAsync(e => e.ConnectionState.IsConnected, cancellationToken);
            var disconnectedCount = totalCount - connectedCount;

            var stateStats = await _context.Equipment
                .GroupBy(e => e.State)
                .ToDictionaryAsync(g => g.Key, g => g.Count(), cancellationToken);

            // 假设有活动报警的统计逻辑
            var equipmentWithAlarmsCount = 0; // 这里需要根据实际的报警实体来计算

            // 需要关注的设备（故障、报警、停机等）
            var equipmentRequiringAttentionCount = await _context.Equipment
                .CountAsync(e => e.State == EquipmentState.FAULT ||
                                e.State == EquipmentState.ALARM ||
                                e.State == EquipmentState.MAINTENANCE, cancellationToken);

            return new EquipmentStatistics(
                totalCount,
                connectedCount,
                disconnectedCount,
                stateStats.AsReadOnly(),
                equipmentWithAlarmsCount,
                equipmentRequiringAttentionCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取设备统计信息失败");
            throw;
        }
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}
