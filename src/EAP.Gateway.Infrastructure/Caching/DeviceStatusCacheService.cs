using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using EAP.Gateway.Core.Models;
using EAP.Gateway.Core.Repositories; // ✅ 引用Core层的接口
using EAP.Gateway.Core.ValueObjects;
using Microsoft.Extensions.Logging;

namespace EAP.Gateway.Infrastructure.Caching;

/// <summary>
/// 设备状态缓存服务实现
/// 实现Core层定义的IDeviceStatusCacheService接口
/// </summary>
public class DeviceStatusCacheService : IDeviceStatusCacheService
{
    private readonly IRedisService _redisService;
    private readonly ILogger<DeviceStatusCacheService> _logger;
    private static readonly TimeSpan _defaultExpiration = TimeSpan.FromMinutes(10);

    public DeviceStatusCacheService(IRedisService redisService, ILogger<DeviceStatusCacheService> logger)
    {
        _redisService = redisService ?? throw new ArgumentNullException(nameof(redisService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<EquipmentStatus?> GetEquipmentStatusAsync(EquipmentId equipmentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = GetStatusCacheKey(equipmentId);
            var status = await _redisService.GetAsync<EquipmentStatus>(cacheKey, cancellationToken);

            if (status != null)
            {
                _logger.LogDebug("从缓存获取设备状态成功: {EquipmentId}", equipmentId.Value);
            }

            return status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从缓存获取设备状态失败: {EquipmentId}", equipmentId.Value);
            return null;
        }
    }

    public async Task<bool> SetEquipmentStatusAsync(EquipmentStatus status, CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = GetStatusCacheKey(status.EquipmentId);
            var success = await _redisService.SetAsync(cacheKey, status, _defaultExpiration, cancellationToken);

            if (success)
            {
                _logger.LogDebug("设备状态缓存更新成功: {EquipmentId}", status.EquipmentId.Value);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "设备状态缓存更新失败: {EquipmentId}", status.EquipmentId.Value);
            return false;
        }
    }

    public async Task<DataVariables?> GetDataVariablesAsync(EquipmentId equipmentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = GetDataVariablesCacheKey(equipmentId);
            var dataVariables = await _redisService.GetAsync<DataVariables>(cacheKey, cancellationToken);

            if (dataVariables != null)
            {
                _logger.LogDebug("从缓存获取数据变量成功: {EquipmentId}", equipmentId.Value);
            }

            return dataVariables;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从缓存获取数据变量失败: {EquipmentId}", equipmentId.Value);
            return null;
        }
    }

    public async Task<bool> SetDataVariablesAsync(DataVariables dataVariables, CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = GetDataVariablesCacheKey(dataVariables.EquipmentId);
            var success = await _redisService.SetAsync(cacheKey, dataVariables, _defaultExpiration, cancellationToken);

            if (success)
            {
                _logger.LogDebug("数据变量缓存更新成功: {EquipmentId}", dataVariables.EquipmentId.Value);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "数据变量缓存更新失败: {EquipmentId}", dataVariables.EquipmentId.Value);
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
                dataVariables = DataVariables.Empty(equipmentId); // ✅ 修复：使用正确的方法签名
            }

            var updatedVariables = dataVariables.UpdateVariable(variableId, value, name); // ✅ 修复：使用领域模型方法
            return await SetDataVariablesAsync(updatedVariables, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新数据变量缓存失败: {EquipmentId}, 变量ID: {VariableId}", equipmentId.Value, variableId);
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
                dataVariables = DataVariables.Empty(equipmentId); // ✅ 修复：使用正确的方法签名
            }

            var updatedVariables = dataVariables.UpdateVariables(updates); // ✅ 修复：使用领域模型方法
            return await SetDataVariablesAsync(updatedVariables, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量更新数据变量缓存失败: {EquipmentId}", equipmentId.Value);
            return false;
        }
    }

    public async Task<bool> RemoveEquipmentStatusAsync(EquipmentId equipmentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var statusKey = GetStatusCacheKey(equipmentId);
            var dataKey = GetDataVariablesCacheKey(equipmentId);

            var tasks = new[]
            {
                _redisService.DeleteAsync(statusKey, cancellationToken),
                _redisService.DeleteAsync(dataKey, cancellationToken)
            };

            var results = await Task.WhenAll(tasks);
            var success = results.All(r => r);

            if (success)
            {
                _logger.LogInformation("设备缓存清除成功: {EquipmentId}", equipmentId.Value);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "设备缓存清除失败: {EquipmentId}", equipmentId.Value);
            return false;
        }
    }

    private static string GetStatusCacheKey(EquipmentId equipmentId) => $"equipment:status:{equipmentId.Value}";
    private static string GetDataVariablesCacheKey(EquipmentId equipmentId) => $"equipment:datavariables:{equipmentId.Value}";


    public async Task<IEnumerable<EquipmentStatus>> GetAllEquipmentStatusAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // 使用通配符模式获取所有设备状态缓存键
            var pattern = "equipment:status:*";
            var keys = await _redisService.GetKeysAsync(pattern, cancellationToken);

            if (!keys.Any())
            {
                _logger.LogInformation("缓存中未找到任何设备状态");
                return Enumerable.Empty<EquipmentStatus>();
            }

            var allStatus = await _redisService.GetMultipleAsync<EquipmentStatus>(keys, cancellationToken);
            var validStatus = allStatus.Values.Where(s => s != null).Cast<EquipmentStatus>();

            _logger.LogDebug("从缓存获取到 {Count} 个设备状态", validStatus.Count());
            return validStatus;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取所有设备状态缓存时发生异常");
            return Enumerable.Empty<EquipmentStatus>();
        }
    }

}
