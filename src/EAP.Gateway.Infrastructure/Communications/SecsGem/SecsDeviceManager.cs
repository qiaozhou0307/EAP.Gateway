using System.Collections.Concurrent;
using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using EAP.Gateway.Core.Services;
using Microsoft.Extensions.Logging;

namespace EAP.Gateway.Infrastructure.Communications.SecsGem;

/// <summary>
/// SECS设备管理器实现
/// </summary>
public class SecsDeviceManager : ISecsDeviceManager
{
    private readonly ILogger<SecsDeviceManager> _logger;
    private readonly ConcurrentDictionary<EquipmentId, ISecsDeviceService> _deviceServices = new();

    public SecsDeviceManager(ILogger<SecsDeviceManager> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ISecsDeviceService?> GetDeviceServiceAsync(EquipmentId equipmentId, CancellationToken cancellationToken = default)
    {
        if (_deviceServices.TryGetValue(equipmentId, out var deviceService))
        {
            _logger.LogDebug("获取设备服务成功 [设备ID: {EquipmentId}]", equipmentId);
            return deviceService;
        }

        _logger.LogDebug("未找到设备服务 [设备ID: {EquipmentId}]", equipmentId);
        return null;
    }

    public async Task<bool> RegisterDeviceServiceAsync(EquipmentId equipmentId, ISecsDeviceService deviceService, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_deviceServices.TryAdd(equipmentId, deviceService))
            {
                _logger.LogInformation("设备服务注册成功 [设备ID: {EquipmentId}]", equipmentId);
                return true;
            }
            else
            {
                _logger.LogWarning("设备服务已存在，注册失败 [设备ID: {EquipmentId}]", equipmentId);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "注册设备服务时发生异常 [设备ID: {EquipmentId}]", equipmentId);
            return false;
        }
    }

    public async Task<bool> UnregisterDeviceServiceAsync(EquipmentId equipmentId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_deviceServices.TryRemove(equipmentId, out var deviceService))
            {
                // 释放设备服务资源
                if (deviceService is IAsyncDisposable asyncDisposable)
                {
                    await asyncDisposable.DisposeAsync();
                }
                else if (deviceService is IDisposable disposable)
                {
                    disposable.Dispose();
                }

                _logger.LogInformation("设备服务注销成功 [设备ID: {EquipmentId}]", equipmentId);
                return true;
            }
            else
            {
                _logger.LogWarning("未找到要注销的设备服务 [设备ID: {EquipmentId}]", equipmentId);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "注销设备服务时发生异常 [设备ID: {EquipmentId}]", equipmentId);
            return false;
        }
    }

    public async Task<IEnumerable<EquipmentId>> GetRegisteredDevicesAsync(CancellationToken cancellationToken = default)
    {
        return _deviceServices.Keys.ToList();
    }

    public async Task<int> GetDeviceCountAsync(CancellationToken cancellationToken = default)
    {
        return _deviceServices.Count;
    }
}
