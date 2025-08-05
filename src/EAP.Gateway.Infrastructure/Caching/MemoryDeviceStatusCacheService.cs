using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using EAP.Gateway.Core.Models;
using EAP.Gateway.Core.Repositories;
using EAP.Gateway.Core.ValueObjects;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace EAP.Gateway.Infrastructure.Caching;

/// <summary>
/// 内存缓存实现的设备状态缓存服务
/// 当Redis不可用时的备用方案
/// </summary>
public class MemoryDeviceStatusCacheService : Core.Repositories.IDeviceStatusCacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<MemoryDeviceStatusCacheService> _logger;
    private static readonly TimeSpan _defaultExpiration = TimeSpan.FromMinutes(10);

    public MemoryDeviceStatusCacheService(IMemoryCache memoryCache, ILogger<MemoryDeviceStatusCacheService> logger)
    {
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<EquipmentStatus?> GetEquipmentStatusAsync(EquipmentId equipmentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = GetStatusCacheKey(equipmentId);
            if (_memoryCache.TryGetValue(key, out EquipmentStatus? status))
            {
                _logger.LogDebug("从内存缓存获取设备状态: {EquipmentId}", equipmentId.Value);
                return status;
            }

            _logger.LogDebug("内存缓存中未找到设备状态: {EquipmentId}", equipmentId.Value);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从内存缓存获取设备状态失败: {EquipmentId}", equipmentId.Value);
            return null;
        }
    }

    public async Task<bool> SetEquipmentStatusAsync(EquipmentStatus status, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = GetStatusCacheKey(status.EquipmentId);
            _memoryCache.Set(key, status, _defaultExpiration);

            _logger.LogDebug("设备状态已写入内存缓存: {EquipmentId}", status.EquipmentId.Value);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "设备状态写入内存缓存失败: {EquipmentId}", status.EquipmentId.Value);
            return false;
        }
    }

    public async Task<DataVariables?> GetDataVariablesAsync(EquipmentId equipmentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = GetDataVariablesCacheKey(equipmentId);
            if (_memoryCache.TryGetValue(key, out DataVariables? dataVariables))
            {
                _logger.LogDebug("从内存缓存获取数据变量: {EquipmentId}", equipmentId.Value);
                return dataVariables;
            }

            _logger.LogDebug("内存缓存中未找到数据变量: {EquipmentId}", equipmentId.Value);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从内存缓存获取数据变量失败: {EquipmentId}", equipmentId.Value);
            return null;
        }
    }

    public async Task<bool> SetDataVariablesAsync(DataVariables dataVariables, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = GetDataVariablesCacheKey(dataVariables.EquipmentId);
            _memoryCache.Set(key, dataVariables, _defaultExpiration);

            _logger.LogDebug("数据变量已写入内存缓存: {EquipmentId}", dataVariables.EquipmentId.Value);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "数据变量写入内存缓存失败: {EquipmentId}", dataVariables.EquipmentId.Value);
            return false;
        }
    }

    public async Task<bool> UpdateDataVariableAsync(EquipmentId equipmentId, uint variableId, object value, string? name = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var dataVariables = await GetDataVariablesAsync(equipmentId, cancellationToken);
            if (dataVariables == null)
            {
                dataVariables = DataVariables.Empty(equipmentId);
            }

            var updatedVariables = dataVariables.UpdateVariable(variableId, value, name);
            return await SetDataVariablesAsync(updatedVariables, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新内存缓存中的数据变量失败: {EquipmentId}, 变量ID: {VariableId}", equipmentId.Value, variableId);
            return false;
        }
    }

    public async Task<bool> UpdateDataVariablesAsync(EquipmentId equipmentId, IReadOnlyDictionary<uint, object> updates, CancellationToken cancellationToken = default)
    {
        try
        {
            var dataVariables = await GetDataVariablesAsync(equipmentId, cancellationToken);
            if (dataVariables == null)
            {
                dataVariables = DataVariables.Empty(equipmentId);
            }

            var updatedVariables = dataVariables.UpdateVariables(updates);
            return await SetDataVariablesAsync(updatedVariables, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量更新内存缓存中的数据变量失败: {EquipmentId}", equipmentId.Value);
            return false;
        }
    }

    public async Task<bool> RemoveEquipmentStatusAsync(EquipmentId equipmentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var statusKey = GetStatusCacheKey(equipmentId);
            var dataKey = GetDataVariablesCacheKey(equipmentId);

            _memoryCache.Remove(statusKey);
            _memoryCache.Remove(dataKey);

            _logger.LogInformation("设备缓存已从内存中清除: {EquipmentId}", equipmentId.Value);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从内存缓存清除设备缓存失败: {EquipmentId}", equipmentId.Value);
            return false;
        }
    }

    public async Task<IEnumerable<EquipmentStatus>> GetAllEquipmentStatusAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // 内存缓存没有像Redis那样的pattern搜索功能
            // 这里返回空集合，实际使用中可以考虑维护一个设备ID列表
            _logger.LogWarning("内存缓存实现不支持获取所有设备状态，返回空集合");
            return Enumerable.Empty<EquipmentStatus>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从内存缓存获取所有设备状态时发生异常");
            return Enumerable.Empty<EquipmentStatus>();
        }
    }

    private static string GetStatusCacheKey(EquipmentId equipmentId) => $"equipment:status:{equipmentId.Value}";
    private static string GetDataVariablesCacheKey(EquipmentId equipmentId) => $"equipment:datavariables:{equipmentId.Value}";
}
