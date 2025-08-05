using System.Collections.Concurrent;
using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using EAP.Gateway.Core.ValueObjects;
using EAP.Gateway.Core.Repositories; // 引用Core层的接口
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EAP.Gateway.Infrastructure.Communications.SecsGem;

/// <summary>
/// SECS设备管理器实现
/// 实现Core层定义的ISecsDeviceManager接口
/// </summary>
public class SecsDeviceManager : ISecsDeviceManager
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IEquipmentRepository _equipmentRepository;
    private readonly ILogger<SecsDeviceManager> _logger;
    private readonly ConcurrentDictionary<EquipmentId, ISecsDeviceService> _deviceServices;
    private readonly SemaphoreSlim _managerSemaphore;
    private volatile bool _disposed = false;

    public SecsDeviceManager(
        IServiceProvider serviceProvider,
        IEquipmentRepository equipmentRepository,
        ILogger<SecsDeviceManager> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _equipmentRepository = equipmentRepository ?? throw new ArgumentNullException(nameof(equipmentRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _deviceServices = new ConcurrentDictionary<EquipmentId, ISecsDeviceService>();
        _managerSemaphore = new SemaphoreSlim(1, 1);
    }

    // ... 其他实现保持不变 ...
    // (使用之前提供的实现代码)

    public async Task<ISecsDeviceService?> GetDeviceServiceAsync(EquipmentId equipmentId, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            return null;

        if (_deviceServices.TryGetValue(equipmentId, out var existingService))
        {
            return existingService;
        }

        await _managerSemaphore.WaitAsync(cancellationToken);
        try
        {
            if (_deviceServices.TryGetValue(equipmentId, out existingService))
            {
                return existingService;
            }

            var equipment = await _equipmentRepository.GetByIdAsync(equipmentId, cancellationToken);
            if (equipment == null)
            {
                _logger.LogWarning("无法找到设备 {EquipmentId}", equipmentId.Value);
                return null;
            }

            using var scope = _serviceProvider.CreateScope();
            var deviceService = scope.ServiceProvider.GetRequiredService<ISecsDeviceService>();

            await deviceService.StartAsync(equipment, cancellationToken);

            _deviceServices.TryAdd(equipmentId, deviceService);
            _logger.LogInformation("创建并启动设备服务 {EquipmentId}", equipmentId.Value);

            return deviceService;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建设备服务失败 {EquipmentId}", equipmentId.Value);
            return null;
        }
        finally
        {
            _managerSemaphore.Release();
        }
    }

    public async Task<IEnumerable<ISecsDeviceService>> GetAllDeviceServicesAsync(CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        return _deviceServices.Values.ToList();
    }

    public async Task<bool> StartDeviceServiceAsync(EquipmentId equipmentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var deviceService = await GetDeviceServiceAsync(equipmentId, cancellationToken);
            return deviceService != null && deviceService.IsStarted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "启动设备服务失败 {EquipmentId}", equipmentId.Value);
            return false;
        }
    }

    public async Task<bool> StopDeviceServiceAsync(EquipmentId equipmentId, string? reason = null, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_deviceServices.TryRemove(equipmentId, out var deviceService))
            {
                await deviceService.StopAsync(reason, cancellationToken);
                await deviceService.DisposeAsync();
                _logger.LogInformation("停止并移除设备服务 {EquipmentId}, 原因: {Reason}", equipmentId.Value, reason);
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "停止设备服务失败 {EquipmentId}", equipmentId.Value);
            return false;
        }
    }

    public async Task<bool> IsDeviceOnlineAsync(EquipmentId equipmentId, CancellationToken cancellationToken = default)
    {
        var deviceService = await GetDeviceServiceAsync(equipmentId, cancellationToken);
        return deviceService?.IsOnline ?? false;
    }

    public async Task<int> GetOnlineDeviceCountAsync(CancellationToken cancellationToken = default)
    {
        var services = await GetAllDeviceServicesAsync(cancellationToken);
        return services.Count(s => s.IsOnline);
    }

    public async Task<bool> RestartDeviceServiceAsync(EquipmentId equipmentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var stopResult = await StopDeviceServiceAsync(equipmentId, "Restart requested", cancellationToken);
            if (!stopResult)
            {
                _logger.LogWarning("重启设备服务时停止失败 {EquipmentId}", equipmentId.Value);
            }

            await Task.Delay(1000, cancellationToken);
            return await StartDeviceServiceAsync(equipmentId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "重启设备服务失败 {EquipmentId}", equipmentId.Value);
            return false;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        var stopTasks = _deviceServices.Values.Select(async service =>
        {
            try
            {
                await service.StopAsync("SecsDeviceManager disposing");
                await service.DisposeAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "释放设备服务时发生异常");
            }
        });

        Task.WaitAll(stopTasks.ToArray(), TimeSpan.FromSeconds(30));

        _deviceServices.Clear();
        _managerSemaphore?.Dispose();

        _logger.LogInformation("SECS设备管理器已释放");
    }
}
