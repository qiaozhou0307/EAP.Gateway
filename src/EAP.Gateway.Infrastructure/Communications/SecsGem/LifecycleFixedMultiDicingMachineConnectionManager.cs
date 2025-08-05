using System.Collections.Concurrent;
using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using EAP.Gateway.Core.Models;
using EAP.Gateway.Core.Repositories;
using EAP.Gateway.Core.Services;
using EAP.Gateway.Core.ValueObjects;
using EAP.Gateway.Infrastructure.Interfaces;
using EAP.Gateway.Infrastructure.Persistence.Factories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EAP.Gateway.Infrastructure.Communications.SecsGem;

/// <summary>
/// 生命周期修复的多台裂片机连接管理器
/// 解决与HostedService的生命周期冲突问题
/// </summary>
public class LifecycleFixedMultiDicingMachineConnectionManager : IMultiDicingMachineConnectionManager, IAsyncInitializable
{
    private readonly ISecsDeviceServiceFactory _deviceServiceFactory;
    private readonly IRepositoryFactory _repositoryFactory;
    private readonly IDbContextScopeFactory _dbContextFactory;
    private readonly ILogger<LifecycleFixedMultiDicingMachineConnectionManager> _logger;
    private readonly ConnectionManagerOptions _options;

    // 设备连接管理
    private readonly ConcurrentDictionary<EquipmentId, DeviceConnection> _activeConnections = new();
    private readonly SemaphoreSlim _connectionSemaphore;

    // 状态管理
    private volatile bool _isInitialized = false;
    private volatile bool _isDisposing = false;
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    // 监控任务
    private Task? _healthMonitoringTask;
    private Task? _connectionCleanupTask;

    // 统计信息
    private readonly ConnectionManagerStatistics _statistics = new();

    public LifecycleFixedMultiDicingMachineConnectionManager(
        ISecsDeviceServiceFactory deviceServiceFactory,
        IRepositoryFactory repositoryFactory,
        IDbContextScopeFactory dbContextFactory,
        IOptions<ConnectionManagerOptions> options,
        ILogger<LifecycleFixedMultiDicingMachineConnectionManager> logger)
    {
        _deviceServiceFactory = deviceServiceFactory ?? throw new ArgumentNullException(nameof(deviceServiceFactory));
        _repositoryFactory = repositoryFactory ?? throw new ArgumentNullException(nameof(repositoryFactory));
        _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _connectionSemaphore = new SemaphoreSlim(_options.MaxConcurrentConnections, _options.MaxConcurrentConnections);

        _logger.LogInformation("多设备连接管理器已创建，最大并发连接数: {MaxConnections}", _options.MaxConcurrentConnections);
    }

    #region IAsyncInitializable 实现

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_isInitialized)
        {
            _logger.LogWarning("连接管理器已经初始化，跳过重复初始化");
            return;
        }

        _logger.LogInformation("初始化多设备连接管理器...");

        try
        {
            // 启动监控任务
            StartBackgroundTasks();

            // 加载现有设备配置
            await LoadExistingDeviceConfigurationsAsync(cancellationToken);

            _isInitialized = true;
            _logger.LogInformation("多设备连接管理器初始化完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "初始化多设备连接管理器失败");
            throw;
        }
    }

    #endregion

    #region 连接管理核心功能

    /// <summary>
    /// 添加并连接裂片机（使用仓储工厂模式）
    /// </summary>
    public async Task<DicingMachineConnectionResult> AddAndConnectDicingMachineAsync(
        string ipAddress,
        int port,
        string? expectedMachineNumber = null,
        TimeSpan? timeout = null)
    {
        ThrowIfDisposing();

        if (!_isInitialized)
        {
            throw new InvalidOperationException("连接管理器尚未初始化");
        }

        var startTime = DateTime.UtcNow;
        var connectionTimeout = timeout ?? TimeSpan.FromSeconds(_options.ConnectionTimeoutSeconds);

        _logger.LogInformation("开始连接裂片机 [IP: {IpAddress}:{Port}, 期望编号: {ExpectedNumber}]",
            ipAddress, port, expectedMachineNumber ?? "未指定");

        // 获取连接信号量
        await _connectionSemaphore.WaitAsync(_cancellationTokenSource.Token);

        try
        {
            _statistics.IncrementConnectionAttempts();

            // 1. 验证输入参数
            ValidateConnectionParameters(ipAddress, port);

            // 2. 检查是否已存在连接
            if (IsDeviceAlreadyConnected(ipAddress, port))
            {
                return DicingMachineConnectionResult.Failed(
                    ipAddress, port, "设备已连接", startTime);
            }

            // 3. 使用DbContext工厂执行数据库操作
            var connectionResult = await _dbContextFactory.ExecuteAsync(async context =>
            {
                return await EstablishDeviceConnectionAsync(ipAddress, port, expectedMachineNumber, connectionTimeout);
            });

            if (connectionResult.IsSuccessful)
            {
                _statistics.IncrementSuccessfulConnections();
            }
            else
            {
                _statistics.IncrementFailedConnections();
            }

            return connectionResult;
        }
        catch (Exception ex)
        {
            _statistics.IncrementFailedConnections();
            _logger.LogError(ex, "连接裂片机异常 [IP: {IpAddress}:{Port}]", ipAddress, port);
            return DicingMachineConnectionResult.Failed(ipAddress, port, ex.Message, startTime);
        }
        finally
        {
            _connectionSemaphore.Release();
        }
    }

    /// <summary>
    /// 并发连接多台裂片机
    /// </summary>
    public async Task<MultiConnectionResult> ConnectMultipleDicingMachinesAsync(
        IEnumerable<DicingMachineConfig> machineConfigs,
        int maxConcurrency = 5)
    {
        ThrowIfDisposing();

        var configs = machineConfigs.ToList();
        var startTime = DateTime.UtcNow;
        var effectiveMaxConcurrency = Math.Min(maxConcurrency, _options.MaxConcurrentConnections);

        _logger.LogInformation("开始并发连接多台裂片机 [数量: {Count}, 最大并发: {MaxConcurrency}]",
            configs.Count, effectiveMaxConcurrency);

        using var semaphore = new SemaphoreSlim(effectiveMaxConcurrency, effectiveMaxConcurrency);

        var connectionTasks = configs.Select(async config =>
        {
            await semaphore.WaitAsync(_cancellationTokenSource.Token);
            try
            {
                return await AddAndConnectDicingMachineAsync(
                    config.IpAddress,
                    config.Port,
                    config.ExpectedMachineNumber,
                    config.ConnectionTimeout);
            }
            finally
            {
                semaphore.Release();
            }
        });

        var connectionResults = await Task.WhenAll(connectionTasks);
        var totalDuration = DateTime.UtcNow - startTime;

        var successCount = connectionResults.Count(r => r.IsSuccessful);
        var failureCount = connectionResults.Count(r => !r.IsSuccessful);

        _logger.LogInformation("多台裂片机连接完成 [成功: {Success}, 失败: {Failed}, 总用时: {Duration}ms]",
            successCount, failureCount, totalDuration.TotalMilliseconds);

        return new MultiConnectionResult(connectionResults, startTime, totalDuration);
    }

    /// <summary>
    /// 断开所有裂片机连接
    /// </summary>
    public async Task DisconnectAllDicingMachinesAsync()
    {
        _logger.LogInformation("开始断开所有裂片机连接...");

        var disconnectionTasks = _activeConnections.Values.Select(async connection =>
        {
            try
            {
                await DisconnectDeviceAsync(connection.EquipmentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "断开设备连接失败 [设备ID: {EquipmentId}]", connection.EquipmentId);
            }
        });

        await Task.WhenAll(disconnectionTasks);

        _activeConnections.Clear();
        _logger.LogInformation("所有裂片机连接已断开");
    }

    /// <summary>
    /// 获取指定裂片机服务
    /// </summary>
    public async Task<ISecsDeviceService?> GetDicingMachineServiceAsync(string machineNumber)
    {
        ThrowIfDisposing();

        if (string.IsNullOrWhiteSpace(machineNumber))
        {
            throw new ArgumentException("机器编号不能为空", nameof(machineNumber));
        }

        var connection = _activeConnections.Values
            .FirstOrDefault(c => c.MachineNumber?.Equals(machineNumber, StringComparison.OrdinalIgnoreCase) == true);

        if (connection != null)
        {
            connection.UpdateActivity();
            return connection.DeviceService;
        }

        // 尝试从数据库查找设备信息
        return await _repositoryFactory.ExecuteAsync(async scope =>
        {
            var equipmentRepo = scope.ServiceProvider.GetRequiredService<IEquipmentRepository>();
            var equipment = await equipmentRepo.GetByMachineNumberAsync(machineNumber);

            if (equipment != null && _activeConnections.TryGetValue(equipment.Id, out var foundConnection))
            {
                foundConnection.UpdateActivity();
                return foundConnection.DeviceService;
            }

            return null;
        });
    }

    /// <summary>
    /// 获取所有裂片机状态
    /// </summary>
    public async Task<IEnumerable<DicingMachineStatus>> GetAllDicingMachineStatusAsync()
    {
        ThrowIfDisposing();

        var statuses = new List<DicingMachineStatus>();

        foreach (var connection in _activeConnections.Values)
        {
            try
            {
                var isHealthy = await CheckConnectionHealthAsync(connection);
                var status = new DicingMachineStatus(
                    connection.EquipmentId,
                    connection.MachineNumber ?? "Unknown",
                    connection.IpAddress,
                    connection.Port,
                    isHealthy,
                    connection.ConnectedAt,
                    connection.LastActivity);

                statuses.Add(status);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "获取设备状态失败 [设备ID: {EquipmentId}]", connection.EquipmentId);

                var errorStatus = new DicingMachineStatus(
                    connection.EquipmentId,
                    connection.MachineNumber ?? "Unknown",
                    connection.IpAddress,
                    connection.Port,
                    false,
                    connection.ConnectedAt,
                    connection.LastActivity);

                statuses.Add(errorStatus);
            }
        }

        return statuses;
    }

    /// <summary>
    /// 获取连接统计信息
    /// </summary>
    public ConnectionStatistics GetConnectionStatistics()
    {
        return new ConnectionStatistics(
            _statistics.TotalConnectionAttempts,
            _statistics.SuccessfulConnections,
            _statistics.FailedConnections,
            _statistics.TotalDisconnections,
            _activeConnections.Count,
            _statistics.ManagerStartedAt);
    }

    /// <summary>
    /// 重连指定裂片机
    /// </summary>
    public async Task<bool> ReconnectDicingMachineAsync(string machineNumber)
    {
        ThrowIfDisposing();

        if (string.IsNullOrWhiteSpace(machineNumber))
        {
            throw new ArgumentException("机器编号不能为空", nameof(machineNumber));
        }

        try
        {
            // 1. 查找现有连接
            var existingConnection = _activeConnections.Values
                .FirstOrDefault(c => c.MachineNumber?.Equals(machineNumber, StringComparison.OrdinalIgnoreCase) == true);

            // 2. 断开现有连接
            if (existingConnection != null)
            {
                _logger.LogInformation("断开现有连接以进行重连 [机器编号: {MachineNumber}]", machineNumber);
                await DisconnectDeviceAsync(existingConnection.EquipmentId);
            }

            // 3. 从数据库获取设备配置
            var deviceConfig = await _repositoryFactory.ExecuteAsync(async scope =>
            {
                var equipmentRepo = scope.ServiceProvider.GetRequiredService<IEquipmentRepository>();
                var equipment = await equipmentRepo.GetByMachineNumberAsync(machineNumber);

                if (equipment?.ConnectionConfig != null)
                {
                    return new DicingMachineConfig(
                        equipment.ConnectionConfig.IpAddress,
                        equipment.ConnectionConfig.Port,
                        machineNumber,
                        TimeSpan.FromSeconds(_options.ConnectionTimeoutSeconds));
                }

                return null;
            });

            if (deviceConfig == null)
            {
                _logger.LogWarning("未找到机器编号为 {MachineNumber} 的设备配置", machineNumber);
                return false;
            }

            // 4. 重新建立连接
            var result = await AddAndConnectDicingMachineAsync(
                deviceConfig.IpAddress,
                deviceConfig.Port,
                machineNumber);

            if (result.IsSuccessful)
            {
                _logger.LogInformation("设备重连成功 [机器编号: {MachineNumber}]", machineNumber);
                return true;
            }
            else
            {
                _logger.LogWarning("设备重连失败 [机器编号: {MachineNumber}, 错误: {Error}]",
                    machineNumber, result.ErrorMessage);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "重连设备异常 [机器编号: {MachineNumber}]", machineNumber);
            return false;
        }
    }

    #endregion

    #region 私有辅助方法

    /// <summary>
    /// 验证连接参数
    /// </summary>
    private static void ValidateConnectionParameters(string ipAddress, int port)
    {
        if (string.IsNullOrWhiteSpace(ipAddress) || !System.Net.IPAddress.TryParse(ipAddress, out _))
        {
            throw new ArgumentException("无效的IP地址", nameof(ipAddress));
        }

        if (port <= 0 || port > 65535)
        {
            throw new ArgumentException("端口必须在1-65535范围内", nameof(port));
        }
    }

    /// <summary>
    /// 检查设备是否已连接
    /// </summary>
    private bool IsDeviceAlreadyConnected(string ipAddress, int port)
    {
        return _activeConnections.Values.Any(c =>
            c.IpAddress.Equals(ipAddress, StringComparison.OrdinalIgnoreCase) && c.Port == port);
    }

    /// <summary>
    /// 建立设备连接
    /// </summary>
    private async Task<DicingMachineConnectionResult> EstablishDeviceConnectionAsync(
        string ipAddress, int port, string? expectedMachineNumber, TimeSpan timeout)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            // 1. 创建设备服务
            var equipmentId = EquipmentId.Create();
            var deviceService = _deviceServiceFactory.CreateDeviceService(equipmentId,
                new DeviceConnectionConfig(ipAddress, port));

            // 2. 建立连接
            var connected = await deviceService.ConnectAsync(_cancellationTokenSource.Token);
            if (!connected)
            {
                return DicingMachineConnectionResult.Failed(ipAddress, port, "无法建立SECS/GEM连接", startTime);
            }

            // 3. 获取设备信息
            var deviceInfo = await deviceService.GetDeviceIdentificationAsync();
            var machineNumber = deviceInfo.ContainsKey("MACHINE_NUMBER") ? deviceInfo["MACHINE_NUMBER"] : $"Unknown_{equipmentId.Value[..8]}";

            // 4. 验证机器编号（如果指定了期望编号）
            if (!string.IsNullOrEmpty(expectedMachineNumber) &&
                !machineNumber.Equals(expectedMachineNumber, StringComparison.OrdinalIgnoreCase))
            {
                await deviceService.DisconnectAsync();
                return DicingMachineConnectionResult.Failed(ipAddress, port,
                    $"机器编号不匹配，期望: {expectedMachineNumber}, 实际: {machineNumber}", startTime);
            }

            // 5. 创建设备连接对象
            var connection = new DeviceConnection(equipmentId, ipAddress, port, deviceService, machineNumber);

            // 6. 存储连接
            _activeConnections.TryAdd(equipmentId, connection);

            var connectionDuration = DateTime.UtcNow - startTime;
            _logger.LogInformation("裂片机连接成功 [编号: {MachineNumber}, IP: {IpAddress}:{Port}, 用时: {Duration}ms]",
                machineNumber, ipAddress, port, connectionDuration.TotalMilliseconds);

            var metadata = new DicingMachineMetadata(machineNumber, deviceInfo);
            return DicingMachineConnectionResult.Successful(equipmentId, metadata, startTime, connectionDuration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "建立设备连接异常 [IP: {IpAddress}:{Port}]", ipAddress, port);
            return DicingMachineConnectionResult.Failed(ipAddress, port, ex.Message, startTime);
        }
    }

    /// <summary>
    /// 断开设备连接
    /// </summary>
    private async Task DisconnectDeviceAsync(EquipmentId equipmentId)
    {
        if (_activeConnections.TryRemove(equipmentId, out var connection))
        {
            try
            {
                await connection.DisposeAsync();
                _statistics.IncrementDisconnections();
                _logger.LogInformation("设备连接已断开 [设备ID: {EquipmentId}]", equipmentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "断开设备连接异常 [设备ID: {EquipmentId}]", equipmentId);
            }
        }
    }

    /// <summary>
    /// 检查连接健康状态
    /// </summary>
    private async Task<bool> CheckConnectionHealthAsync(DeviceConnection connection)
    {
        try
        {
            // 发送心跳消息检查连接状态
            return await connection.DeviceService.IsConnectedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "健康检查失败 [设备ID: {EquipmentId}]", connection.EquipmentId);
            return false;
        }
    }

    /// <summary>
    /// 加载现有设备配置
    /// </summary>
    private async Task LoadExistingDeviceConfigurationsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var deviceConfigs = await _repositoryFactory.ExecuteAsync(async scope =>
            {
                var equipmentRepo = scope.ServiceProvider.GetRequiredService<IEquipmentRepository>();
                var equipments = await equipmentRepo.GetAllActiveAsync();

                return equipments
                    .Where(e => e.ConnectionConfig != null)
                    .Select(e => new DicingMachineConfig(
                        e.ConnectionConfig!.IpAddress,
                        e.ConnectionConfig.Port,
                        e.Name,
                        TimeSpan.FromSeconds(_options.ConnectionTimeoutSeconds)))
                    .ToList();
            });

            if (deviceConfigs.Any())
            {
                _logger.LogInformation("开始连接 {Count} 台已配置的设备", deviceConfigs.Count);

                var result = await ConnectMultipleDicingMachinesAsync(deviceConfigs, _options.MaxConcurrentConnections);

                _logger.LogInformation("设备连接完成 [成功: {Success}, 失败: {Failed}]",
                    result.SuccessfulConnections.Count(), result.FailedConnections.Count());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载现有设备配置失败");
        }
    }

    /// <summary>
    /// 启动后台任务
    /// </summary>
    private void StartBackgroundTasks()
    {
        _healthMonitoringTask = MonitorConnectionHealthAsync(_cancellationTokenSource.Token);
        _connectionCleanupTask = PerformConnectionCleanupAsync(_cancellationTokenSource.Token);
    }

    /// <summary>
    /// 健康监控任务
    /// </summary>
    private async Task MonitorConnectionHealthAsync(CancellationToken cancellationToken)
    {
        var interval = TimeSpan.FromSeconds(_options.HealthCheckIntervalSeconds);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(interval, cancellationToken);
                await PerformHealthCheckOnAllConnectionsAsync();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "健康监控任务异常");
            }
        }
    }

    /// <summary>
    /// 连接清理任务
    /// </summary>
    private async Task PerformConnectionCleanupAsync(CancellationToken cancellationToken)
    {
        var interval = TimeSpan.FromMinutes(_options.CleanupIntervalMinutes);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(interval, cancellationToken);
                await CleanupStaleConnectionsAsync();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "连接清理任务异常");
            }
        }
    }

    /// <summary>
    /// 对所有连接执行健康检查
    /// </summary>
    private async Task PerformHealthCheckOnAllConnectionsAsync()
    {
        var healthCheckTasks = _activeConnections.Values.Select(async connection =>
        {
            try
            {
                var isHealthy = await CheckConnectionHealthAsync(connection);
                connection.UpdateHealthStatus(isHealthy);

                if (!isHealthy)
                {
                    _logger.LogWarning("设备连接不健康 [设备ID: {EquipmentId}, 机器编号: {MachineNumber}]",
                        connection.EquipmentId, connection.MachineNumber);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "健康检查异常 [设备ID: {EquipmentId}]", connection.EquipmentId);
                connection.UpdateHealthStatus(false);
            }
        });

        await Task.WhenAll(healthCheckTasks);
    }

    /// <summary>
    /// 清理过期连接
    /// </summary>
    private async Task CleanupStaleConnectionsAsync()
    {
        var staleThreshold = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(_options.StaleConnectionThresholdMinutes));
        var staleConnections = _activeConnections.Values
            .Where(c => c.LastActivity < staleThreshold && !c.IsHealthy)
            .ToList();

        foreach (var staleConnection in staleConnections)
        {
            _logger.LogWarning("清理过期连接 [设备ID: {EquipmentId}, 机器编号: {MachineNumber}]",
                staleConnection.EquipmentId, staleConnection.MachineNumber);

            await DisconnectDeviceAsync(staleConnection.EquipmentId);
        }

        if (staleConnections.Any())
        {
            _logger.LogInformation("已清理 {Count} 个过期连接", staleConnections.Count);
        }
    }

    /// <summary>
    /// 检查是否正在释放资源
    /// </summary>
    private void ThrowIfDisposing()
    {
        if (_isDisposing)
        {
            throw new ObjectDisposedException(nameof(LifecycleFixedMultiDicingMachineConnectionManager));
        }
    }

    #endregion

    #region 资源释放

    public async ValueTask DisposeAsync()
    {
        if (_isDisposing) return;

        _isDisposing = true;
        _logger.LogInformation("开始释放多设备连接管理器资源...");

        try
        {
            // 1. 取消所有后台任务
            _cancellationTokenSource.Cancel();

            // 2. 等待后台任务完成
            var tasks = new[] { _healthMonitoringTask, _connectionCleanupTask }
                .Where(t => t != null).Cast<Task>().ToArray();

            if (tasks.Any())
            {
                await Task.WhenAll(tasks);
            }

            // 3. 断开所有设备连接
            await DisconnectAllDicingMachinesAsync();

            // 4. 释放其他资源
            _connectionSemaphore.Dispose();
            _cancellationTokenSource.Dispose();

            _logger.LogInformation("多设备连接管理器资源释放完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "释放资源时发生异常");
        }
    }

    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    #endregion
}

/// <summary>
/// 改进的设备连接对象
/// </summary>
public class DeviceConnection : IAsyncDisposable
{
    public EquipmentId EquipmentId { get; }
    public string IpAddress { get; }
    public int Port { get; }
    public string? MachineNumber { get; }
    public ISecsDeviceService DeviceService { get; }
    public DateTime ConnectedAt { get; }
    public DateTime LastActivity { get; private set; }
    public bool IsHealthy { get; private set; }

    public DeviceConnection(EquipmentId equipmentId, string ipAddress, int port,
        ISecsDeviceService deviceService, string? machineNumber = null)
    {
        EquipmentId = equipmentId;
        IpAddress = ipAddress;
        Port = port;
        MachineNumber = machineNumber;
        DeviceService = deviceService;
        ConnectedAt = DateTime.UtcNow;
        LastActivity = DateTime.UtcNow;
        IsHealthy = true;
    }

    public void UpdateActivity() => LastActivity = DateTime.UtcNow;
    public void UpdateHealthStatus(bool isHealthy) => IsHealthy = isHealthy;

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (DeviceService is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
            else if (DeviceService is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
        catch (Exception)
        {
            // 忽略释放时的异常
        }
    }
}

/// <summary>
/// 连接管理器统计信息
/// </summary>
public class ConnectionManagerStatistics
{
    private long _totalConnectionAttempts = 0;
    private long _successfulConnections = 0;
    private long _failedConnections = 0;
    private long _totalDisconnections = 0;

    public long TotalConnectionAttempts => _totalConnectionAttempts;
    public long SuccessfulConnections => _successfulConnections;
    public long FailedConnections => _failedConnections;
    public long TotalDisconnections => _totalDisconnections;
    public DateTime ManagerStartedAt { get; } = DateTime.UtcNow;

    public void IncrementConnectionAttempts() => Interlocked.Increment(ref _totalConnectionAttempts);
    public void IncrementSuccessfulConnections() => Interlocked.Increment(ref _successfulConnections);
    public void IncrementFailedConnections() => Interlocked.Increment(ref _failedConnections);
    public void IncrementDisconnections() => Interlocked.Increment(ref _totalDisconnections);
}
