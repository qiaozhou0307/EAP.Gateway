// src/EAP.Gateway.Infrastructure/Communications/SecsGem/MultiDicingMachineConnectionManager.cs
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using EAP.Gateway.Core.Events.Alarm;
using EAP.Gateway.Core.Events.Equipment;
using EAP.Gateway.Core.Models;
using EAP.Gateway.Core.Repositories;
using EAP.Gateway.Core.Services;
using EAP.Gateway.Core.ValueObjects;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Secs4Net;

namespace EAP.Gateway.Infrastructure.Communications.SecsGem;

/// <summary>
/// 多台裂片机连接管理器
/// 负责管理与多台裂片机的并发连接、设备信息获取和安全存储
/// </summary>
public class MultiDicingMachineConnectionManager : IMultiDicingMachineConnectionManager, IHostedService
{
    private readonly ISecsDeviceServiceFactory _deviceServiceFactory;
    private readonly IEquipmentRepository _equipmentRepository;
    private readonly IMediator _mediator;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MultiDicingMachineConnectionManager> _logger;

    // 设备服务字典 - 线程安全的并发字典
    private readonly ConcurrentDictionary<EquipmentId, ISecsDeviceService> _deviceServices = new();

    // 裂片机特定信息存储
    private readonly ConcurrentDictionary<EquipmentId, DicingMachineMetadata> _dicingMachineMetadata = new();

    // 连接任务追踪
    private readonly ConcurrentDictionary<EquipmentId, Task<bool>> _connectionTasks = new();

    // 同步控制
    private readonly SemaphoreSlim _managementSemaphore = new(1, 1);
    private readonly CancellationTokenSource _serviceCts = new();

    // 状态管理
    private volatile bool _isStarted = false;
    private Task? _healthMonitoringTask;
    private Task? _connectionMonitoringTask;

    // 统计信息
    private int _totalConnectionAttempts = 0;
    private int _successfulConnections = 0;
    private DateTime _managerStartTime = DateTime.UtcNow;

    public MultiDicingMachineConnectionManager(
        ISecsDeviceServiceFactory deviceServiceFactory,
        IEquipmentRepository equipmentRepository,
        IMediator mediator,
        IConfiguration configuration,
        ILogger<MultiDicingMachineConnectionManager> logger)
    {
        _deviceServiceFactory = deviceServiceFactory ?? throw new ArgumentNullException(nameof(deviceServiceFactory));
        _equipmentRepository = equipmentRepository ?? throw new ArgumentNullException(nameof(equipmentRepository));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region IHostedService Implementation

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("🚀 多台裂片机连接管理器启动中...");

        _isStarted = true;
        _managerStartTime = DateTime.UtcNow;

        // 从配置加载并连接所有裂片机
        await LoadAndConnectDicingMachinesFromConfigAsync(cancellationToken);

        // 启动监控任务
        _healthMonitoringTask = StartHealthMonitoringAsync(_serviceCts.Token);
        _connectionMonitoringTask = StartConnectionMonitoringAsync(_serviceCts.Token);

        _logger.LogInformation("✅ 多台裂片机连接管理器已启动，管理 {Count} 台设备", _deviceServices.Count);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("🛑 多台裂片机连接管理器停止中...");

        _isStarted = false;

        // 停止监控任务
        _serviceCts.Cancel();

        // 等待监控任务完成
        if (_healthMonitoringTask != null)
            await _healthMonitoringTask;
        if (_connectionMonitoringTask != null)
            await _connectionMonitoringTask;

        // 断开所有设备连接
        await DisconnectAllDicingMachinesAsync();

        _logger.LogInformation("✅ 多台裂片机连接管理器已停止");
    }

    #endregion

    #region 核心连接管理

    /// <summary>
    /// 添加并连接裂片机
    /// </summary>
    /// <param name="ipAddress">设备IP地址</param>
    /// <param name="port">设备端口</param>
    /// <param name="expectedMachineNumber">期望的裂片机编号</param>
    /// <param name="timeout">连接超时时间</param>
    /// <returns>连接结果</returns>
    public async Task<DicingMachineConnectionResult> AddAndConnectDicingMachineAsync(
        string ipAddress,
        int port,
        string? expectedMachineNumber = null,
        TimeSpan? timeout = null)
    {
        // 验证输入参数
        if (string.IsNullOrWhiteSpace(ipAddress) || !IPAddress.TryParse(ipAddress, out _))
        {
            throw new ArgumentException("无效的IP地址", nameof(ipAddress));
        }

        if (port <= 0 || port > 65535)
        {
            throw new ArgumentException("端口必须在1-65535范围内", nameof(port));
        }

        var connectionTimeout = timeout ?? TimeSpan.FromSeconds(30);
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("🔌 开始连接裂片机 [IP: {IpAddress}:{Port}, 期望编号: {ExpectedNumber}]",
            ipAddress, port, expectedMachineNumber ?? "未指定");

        Interlocked.Increment(ref _totalConnectionAttempts);

        try
        {
            await _managementSemaphore.WaitAsync();

            // 步骤1: 建立基础TCP连接并获取设备信息
            var deviceInfo = await EstablishConnectionAndGetDeviceInfoAsync(ipAddress, port, connectionTimeout);

            if (!deviceInfo.IsSuccessful)
            {
                return DicingMachineConnectionResult.Failed(
                    ipAddress, port, deviceInfo.ErrorMessage, startTime);
            }

            // 步骤2: 验证设备是否为裂片机
            if (!ValidateDicingMachine(deviceInfo.DeviceIdentification))
            {
                return DicingMachineConnectionResult.Failed(
                    ipAddress, port, "设备不是裂片机类型", startTime);
            }

            // 步骤3: 安全获取裂片机编号和版本
            var machineMetadata = await SafelyExtractMachineMetadataAsync(deviceInfo);

            // 步骤4: 验证编号匹配（如果提供了期望编号）
            if (!string.IsNullOrWhiteSpace(expectedMachineNumber) &&
                !string.Equals(machineMetadata.MachineNumber, expectedMachineNumber, StringComparison.OrdinalIgnoreCase))
            {
                return DicingMachineConnectionResult.Failed(
                    ipAddress, port,
                    $"裂片机编号不匹配：期望 {expectedMachineNumber}，实际 {machineMetadata.MachineNumber}",
                    startTime);
            }

            // 步骤5: 创建设备配置
            var equipmentConfig = CreateEquipmentConfiguration(ipAddress, port, machineMetadata);

            // 步骤6: 创建设备ID
            var equipmentId = EquipmentId.Create($"DICER_{machineMetadata.MachineNumber}");

            // 步骤7: 检查是否已存在
            if (_deviceServices.ContainsKey(equipmentId))
            {
                return DicingMachineConnectionResult.Failed(
                    ipAddress, port, $"裂片机 {machineMetadata.MachineNumber} 已连接", startTime);
            }

            // 步骤8: 创建并启动设备服务
            var deviceService = _deviceServiceFactory.CreateDeviceService(equipmentId, equipmentConfig);

            // 步骤9: 创建或获取设备聚合根
            var equipment = await GetOrCreateEquipmentAsync(equipmentId, machineMetadata, equipmentConfig);

            // 步骤10: 启动设备服务
            await deviceService.StartAsync(equipment);

            // 步骤11: 建立SECS/GEM连接
            var connected = await deviceService.ConnectAsync();

            if (!connected)
            {
                await deviceService.StopAsync();
                return DicingMachineConnectionResult.Failed(
                    ipAddress, port, "SECS/GEM连接失败", startTime);
            }

            // 步骤12: 存储设备服务和元数据
            _deviceServices[equipmentId] = deviceService;
            _dicingMachineMetadata[equipmentId] = machineMetadata;

            // 步骤13: 订阅设备事件
            SubscribeToDeviceEvents(deviceService);

            Interlocked.Increment(ref _successfulConnections);

            var connectionDuration = DateTime.UtcNow - startTime;

            _logger.LogInformation("✅ 裂片机连接成功 [编号: {MachineNumber}, 版本: {Version}, 用时: {Duration}ms]",
                machineMetadata.MachineNumber, machineMetadata.Version, connectionDuration.TotalMilliseconds);

            return DicingMachineConnectionResult.Successful(
                equipmentId, machineMetadata, startTime, connectionDuration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 连接裂片机异常 [IP: {IpAddress}:{Port}]", ipAddress, port);
            return DicingMachineConnectionResult.Failed(ipAddress, port, ex.Message, startTime);
        }
        finally
        {
            _managementSemaphore.Release();
        }
    }

    /// <summary>
    /// 并发连接多台裂片机
    /// </summary>
    /// <param name="machineConfigs">裂片机配置列表</param>
    /// <param name="maxConcurrency">最大并发连接数</param>
    /// <returns>连接结果汇总</returns>
    public async Task<MultiConnectionResult> ConnectMultipleDicingMachinesAsync(
        IEnumerable<DicingMachineConfig> machineConfigs,
        int maxConcurrency = 5)
    {
        var configs = machineConfigs.ToList();
        var results = new List<DicingMachineConnectionResult>();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("🔄 开始并发连接多台裂片机 [数量: {Count}, 最大并发: {MaxConcurrency}]",
            configs.Count, maxConcurrency);

        using var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);

        var connectionTasks = configs.Select(async config =>
        {
            await semaphore.WaitAsync();
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
        results.AddRange(connectionResults);

        var successCount = results.Count(r => r.IsSuccessful);
        var failureCount = results.Count(r => !r.IsSuccessful);
        var totalDuration = DateTime.UtcNow - startTime;

        _logger.LogInformation("📊 多台裂片机连接完成 [成功: {Success}, 失败: {Failed}, 总用时: {Duration}ms]",
            successCount, failureCount, totalDuration.TotalMilliseconds);

        return new MultiConnectionResult(results, startTime, totalDuration);
    }

    #endregion

    #region 设备信息获取与验证

    /// <summary>
    /// 建立连接并获取设备信息
    /// </summary>
    private async Task<DeviceInfoResult> EstablishConnectionAndGetDeviceInfoAsync(
        string ipAddress, int port, TimeSpan timeout)
    {
        try
        {
            _logger.LogDebug("🔍 建立临时连接以获取设备信息 [IP: {IpAddress}:{Port}]", ipAddress, port);

            // 创建临时SECS/GEM连接用于设备识别
            var tempConfig = CreateTemporaryConfiguration(ipAddress, port);
            using var tempSecsGem = new SecsGem(tempConfig);

            var connectTask = tempSecsGem.GetPrimaryMessageAsync().GetAsyncEnumerator().MoveNextAsync();
            var timeoutTask = Task.Delay(timeout);

            // 等待连接建立或超时
            var completedTask = await Task.WhenAny(connectTask.AsTask(), timeoutTask);

            if (completedTask == timeoutTask)
            {
                return DeviceInfoResult.Failed("连接超时");
            }

            // 发送设备识别请求 (S1F13 - 设备信息请求)
            var deviceInfoRequest = new SecsMessage(1, 13, replyExpected: true);
            deviceInfoRequest.SecsItem = Item.L(); // 空列表表示请求所有信息

            var response = await tempSecsGem.SendAsync(deviceInfoRequest);

            if (response?.S == 1 && response?.F == 14)
            {
                var deviceInfo = ParseDeviceIdentificationResponse(response);
                return DeviceInfoResult.Successful(deviceInfo);
            }

            return DeviceInfoResult.Failed("无法获取设备信息");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "获取设备信息失败 [IP: {IpAddress}:{Port}]", ipAddress, port);
            return DeviceInfoResult.Failed($"连接异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 安全提取裂片机元数据
    /// </summary>
    private async Task<DicingMachineMetadata> SafelyExtractMachineMetadataAsync(DeviceInfoResult deviceInfo)
    {
        try
        {
            var identification = deviceInfo.DeviceIdentification;

            // 提取裂片机编号 - 通过设备模型名称解析
            var machineNumber = ExtractMachineNumber(identification);

            // 提取裂片机版本 - 通过软件版本或设备版本
            var machineVersion = ExtractMachineVersion(identification);

            // 提取制造商信息
            var manufacturer = identification.GetValueOrDefault("MANUFACTURER", "Unknown");

            // 提取型号信息
            var model = identification.GetValueOrDefault("MODEL", "Unknown");

            // 提取序列号
            var serialNumber = identification.GetValueOrDefault("SERIAL_NUMBER", "Unknown");

            // 验证关键信息
            ValidateMachineMetadata(machineNumber, machineVersion);

            var metadata = new DicingMachineMetadata(
                machineNumber: machineNumber,
                version: machineVersion,
                manufacturer: manufacturer,
                model: model,
                serialNumber: serialNumber,
                registeredAt: DateTime.UtcNow,
                capabilities: ExtractCapabilities(identification),
                extendedProperties: ExtractExtendedProperties(identification));

            _logger.LogInformation("📋 成功提取裂片机元数据 [编号: {Number}, 版本: {Version}, 制造商: {Manufacturer}]",
                machineNumber, machineVersion, manufacturer);

            return metadata;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "提取裂片机元数据失败");
            throw new InvalidOperationException("无法提取有效的裂片机元数据", ex);
        }
    }

    /// <summary>
    /// 提取裂片机编号
    /// </summary>
    private string ExtractMachineNumber(Dictionary<string, string> identification)
    {
        // 尝试多种方式提取机器编号
        var possibleKeys = new[] { "MACHINE_NUMBER", "EQUIPMENT_ID", "STATION_NAME", "MODEL_NAME" };

        foreach (var key in possibleKeys)
        {
            if (identification.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                // 提取编号部分 (例如: "DICER_001" -> "001")
                var match = System.Text.RegularExpressions.Regex.Match(value, @"(?:DICER_)?(\d{3,})");
                if (match.Success)
                {
                    return match.Groups[1].Value.PadLeft(3, '0'); // 确保至少3位数字
                }
            }
        }

        // 如果无法从设备信息中提取，生成默认编号
        var timestamp = DateTime.UtcNow.ToString("HHmmss");
        var defaultNumber = $"AUTO_{timestamp}";

        _logger.LogWarning("⚠️ 无法从设备信息中提取裂片机编号，使用默认编号: {DefaultNumber}", defaultNumber);
        return defaultNumber;
    }

    /// <summary>
    /// 提取裂片机版本
    /// </summary>
    private string ExtractMachineVersion(Dictionary<string, string> identification)
    {
        // 尝试多种方式提取版本信息
        var possibleKeys = new[] { "SOFTWARE_VERSION", "FIRMWARE_VERSION", "DEVICE_VERSION", "VERSION" };

        foreach (var key in possibleKeys)
        {
            if (identification.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                // 验证版本格式并标准化
                var version = NormalizeVersion(value);
                if (!string.IsNullOrWhiteSpace(version))
                {
                    return version;
                }
            }
        }

        // 默认版本
        var defaultVersion = "V1.0.0";
        _logger.LogWarning("⚠️ 无法从设备信息中提取版本，使用默认版本: {DefaultVersion}", defaultVersion);
        return defaultVersion;
    }

    /// <summary>
    /// 标准化版本格式
    /// </summary>
    private string NormalizeVersion(string rawVersion)
    {
        if (string.IsNullOrWhiteSpace(rawVersion))
            return "V1.0.0";

        // 移除前缀并提取版本号
        var cleanVersion = rawVersion.Trim().ToUpperInvariant();

        // 如果已经是标准格式 (VX.Y.Z)，直接返回
        if (System.Text.RegularExpressions.Regex.IsMatch(cleanVersion, @"^V\d+\.\d+\.\d+$"))
        {
            return cleanVersion;
        }

        // 尝试提取版本数字 (例如: "2.1.0", "v2.1", "Ver2.1.0")
        var versionMatch = System.Text.RegularExpressions.Regex.Match(cleanVersion, @"(\d+)\.?(\d+)?\.?(\d+)?");
        if (versionMatch.Success)
        {
            var major = versionMatch.Groups[1].Value;
            var minor = versionMatch.Groups[2].Success ? versionMatch.Groups[2].Value : "0";
            var patch = versionMatch.Groups[3].Success ? versionMatch.Groups[3].Value : "0";

            return $"V{major}.{minor}.{patch}";
        }

        return "V1.0.0";
    }

    /// <summary>
    /// 验证设备是否为裂片机
    /// </summary>
    private bool ValidateDicingMachine(Dictionary<string, string> identification)
    {
        // 检查设备类型标识
        var deviceTypeKeys = new[] { "DEVICE_TYPE", "EQUIPMENT_TYPE", "MACHINE_TYPE" };

        foreach (var key in deviceTypeKeys)
        {
            if (identification.TryGetValue(key, out var deviceType))
            {
                var type = deviceType.ToUpperInvariant();
                if (type.Contains("DICER") || type.Contains("DICING") || type.Contains("SAW"))
                {
                    return true;
                }
            }
        }

        // 检查型号名称
        var modelKeys = new[] { "MODEL", "MODEL_NAME", "EQUIPMENT_NAME" };

        foreach (var key in modelKeys)
        {
            if (identification.TryGetValue(key, out var model))
            {
                var modelName = model.ToUpperInvariant();
                if (modelName.Contains("DICER") || modelName.Contains("DICING"))
                {
                    return true;
                }
            }
        }

        _logger.LogWarning("⚠️ 设备验证警告：无法确认设备类型为裂片机，但将继续连接");
        return true; // 宽松验证，允许连接
    }

    /// <summary>
    /// 获取或创建设备聚合根
    /// </summary>
    private async Task<Equipment> GetOrCreateEquipmentAsync(
        EquipmentId equipmentId,
        DicingMachineMetadata metadata,
        EquipmentConfiguration config)
    {
        // 尝试从仓储获取现有设备
        var existingEquipment = await _equipmentRepository.GetByIdAsync(equipmentId);

        if (existingEquipment != null)
        {
            // 更新设备基础信息
            existingEquipment.UpdateBasicInfo(
                manufacturer: metadata.Manufacturer,
                model: metadata.Model,
                serialNumber: metadata.SerialNumber,
                updatedBy: "ConnectionManager");

            await _equipmentRepository.UpdateAsync(existingEquipment);

            _logger.LogInformation("📝 更新现有设备信息 [设备: {EquipmentId}]", equipmentId);
            return existingEquipment;
        }

        // 创建新设备聚合根
        var newEquipment = Equipment.Create(
            equipmentId: equipmentId,
            name: $"裂片机_{metadata.MachineNumber}",
            description: $"裂片机设备 - 版本 {metadata.Version}",
            configuration: config,
            createdBy: "ConnectionManager");

        // 设置设备基础信息
        newEquipment.UpdateBasicInfo(
            manufacturer: metadata.Manufacturer,
            model: metadata.Model,
            serialNumber: metadata.SerialNumber,
            dataCollectionInterval: 1, // 1秒数据采集间隔
            enableAlarmCollection: true,
            updatedBy: "ConnectionManager");

        await _equipmentRepository.AddAsync(newEquipment);

        _logger.LogInformation("🆕 创建新设备聚合根 [设备: {EquipmentId}]", equipmentId);
        return newEquipment;
    }

    #endregion

    #region 配置和辅助方法

    /// <summary>
    /// 从配置文件加载并连接裂片机
    /// </summary>
    private async Task LoadAndConnectDicingMachinesFromConfigAsync(CancellationToken cancellationToken)
    {
        try
        {
            var deviceConfigs = _configuration.GetSection("DicingMachines:Devices")
                .Get<DicingMachineConfig[]>() ?? Array.Empty<DicingMachineConfig>();

            if (!deviceConfigs.Any())
            {
                _logger.LogWarning("⚠️ 配置文件中未找到裂片机配置");
                return;
            }

            _logger.LogInformation("📋 从配置加载 {Count} 台裂片机", deviceConfigs.Length);

            // 并发连接所有配置的裂片机
            var result = await ConnectMultipleDicingMachinesAsync(deviceConfigs, maxConcurrency: 3);

            _logger.LogInformation("📈 配置加载完成 [成功率: {SuccessRate:F1}%]", result.SuccessRate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 从配置加载裂片机失败");
        }
    }

    /// <summary>
    /// 创建设备配置
    /// </summary>
    private EquipmentConfiguration CreateEquipmentConfiguration(
        string ipAddress, int port, DicingMachineMetadata metadata)
    {
        var endpoint = new IpEndpoint(ipAddress, port);
        var timeouts = HsmsTimeouts.Default();

        return new EquipmentConfiguration(
            endpoint: endpoint,
            timeouts: timeouts,
            connectionMode: ConnectionMode.Active,
            enableAutoReconnect: true,
            heartbeatInterval: 30,
            enableDataCollection: true,
            enableAlarmHandling: true,
            enableRemoteControl: true);
    }

    /// <summary>
    /// 创建临时配置用于设备识别
    /// </summary>
    private SecsGemOptions CreateTemporaryConfiguration(string ipAddress, int port)
    {
        return new SecsGemOptions
        {
            IpAddress = ipAddress,
            Port = port,
            IsActive = true,
            T3 = TimeSpan.FromSeconds(45),
            T5 = TimeSpan.FromSeconds(10),
            T6 = TimeSpan.FromSeconds(5),
            T7 = TimeSpan.FromSeconds(10),
            T8 = TimeSpan.FromSeconds(6)
        };
    }

    /// <summary>
    /// 解析设备识别响应
    /// </summary>
    private Dictionary<string, string> ParseDeviceIdentificationResponse(SecsMessage response)
    {
        var identification = new Dictionary<string, string>();

        try
        {
            if (response.SecsItem?.Items != null)
            {
                // S1F14 响应通常包含设备信息列表
                for (int i = 0; i < response.SecsItem.Items.Length; i += 2)
                {
                    if (i + 1 < response.SecsItem.Items.Length)
                    {
                        var key = response.SecsItem.Items[i].GetValue<string>() ?? $"KEY_{i}";
                        var value = response.SecsItem.Items[i + 1].GetValue<string>() ?? "";
                        identification[key.ToUpperInvariant()] = value;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "解析设备识别响应失败");
        }

        return identification;
    }

    /// <summary>
    /// 订阅设备事件
    /// </summary>
    private void SubscribeToDeviceEvents(ISecsDeviceService deviceService)
    {
        // 连接状态变化事件
        deviceService.HsmsClient.ConnectionStateChanged += async (sender, args) =>
        {
            await HandleConnectionStateChangedAsync(deviceService.EquipmentId, args);
        };

        // 数据接收事件
        deviceService.DataReceived += async (sender, args) =>
        {
            await HandleDataReceivedAsync(deviceService.EquipmentId, args);
        };

        // 报警事件
        deviceService.AlarmEvent += async (sender, args) =>
        {
            await HandleAlarmEventAsync(deviceService.EquipmentId, args);
        };
    }

    #endregion

    #region 监控任务

    /// <summary>
    /// 启动健康监控
    /// </summary>
    private async Task StartHealthMonitoringAsync(CancellationToken cancellationToken)
    {
        const int monitoringIntervalMinutes = 2;

        while (!cancellationToken.IsCancellationRequested && _isStarted)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(monitoringIntervalMinutes), cancellationToken);
                await PerformHealthCheckOnAllDevicesAsync();
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
    /// 启动连接监控
    /// </summary>
    private async Task StartConnectionMonitoringAsync(CancellationToken cancellationToken)
    {
        const int monitoringIntervalSeconds = 30;

        while (!cancellationToken.IsCancellationRequested && _isStarted)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(monitoringIntervalSeconds), cancellationToken);
                await MonitorConnectionsAsync();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "连接监控任务异常");
            }
        }
    }

    #endregion

    #region 查询和统计

    /// <summary>
    /// 获取所有裂片机状态
    /// </summary>
    public async Task<IEnumerable<DicingMachineStatus>> GetAllDicingMachineStatusAsync()
    {
        var statusList = new List<DicingMachineStatus>();

        foreach (var kvp in _deviceServices)
        {
            var equipmentId = kvp.Key;
            var deviceService = kvp.Value;
            var metadata = _dicingMachineMetadata.GetValueOrDefault(equipmentId);

            var status = new DicingMachineStatus
            {
                EquipmentId = equipmentId,
                MachineNumber = metadata?.MachineNumber ?? "Unknown",
                MachineVersion = metadata?.Version ?? "Unknown",
                Manufacturer = metadata?.Manufacturer ?? "Unknown",
                Model = metadata?.Model ?? "Unknown",
                SerialNumber = metadata?.SerialNumber ?? "Unknown",
                IsConnected = deviceService.HsmsClient.IsConnected,
                IsOnline = deviceService.IsOnline,
                HealthStatus = deviceService.HealthStatus,
                ConnectionState = deviceService.Equipment?.State ?? EquipmentState.UNKNOWN,
                LastHeartbeat = deviceService.HsmsClient.LastHeartbeat,
                RegisteredAt = metadata?.RegisteredAt ?? DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            statusList.Add(status);
        }

        return statusList;
    }

    /// <summary>
    /// 获取连接统计信息
    /// </summary>
    public ConnectionStatistics GetConnectionStatistics()
    {
        var totalDevices = _deviceServices.Count;
        var connectedDevices = _deviceServices.Values.Count(d => d.HsmsClient.IsConnected);
        var onlineDevices = _deviceServices.Values.Count(d => d.IsOnline);
        var uptime = DateTime.UtcNow - _managerStartTime;

        return new ConnectionStatistics(
            totalDevices: totalDevices,
            connectedDevices: connectedDevices,
            onlineDevices: onlineDevices,
            connectionRate: totalDevices > 0 ? (double)connectedDevices / totalDevices * 100 : 0,
            successRate: _totalConnectionAttempts > 0 ? (double)_successfulConnections / _totalConnectionAttempts * 100 : 0,
            uptime: uptime,
            statisticsTime: DateTime.UtcNow);
    }

    #endregion

    #region 辅助方法实现

    /// <summary>
    /// 提取设备能力特性
    /// </summary>
    private DicingMachineCapabilities ExtractCapabilities(Dictionary<string, string> identification)
    {
        try
        {
            // 从设备识别信息中提取能力参数
            var maxWaferSize = ExtractDoubleValue(identification, "MAX_WAFER_SIZE", 8.0);
            var maxCuttingSpeed = ExtractDoubleValue(identification, "MAX_CUTTING_SPEED", 50.0);
            var accuracyLevel = ExtractDoubleValue(identification, "ACCURACY_LEVEL", 1.0);

            // 提取支持的切割类型
            var cuttingTypesStr = identification.GetValueOrDefault("SUPPORTED_CUTTING_TYPES", "Full Cut,Half Cut");
            var supportedCuttingTypes = cuttingTypesStr.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim()).ToList();

            // 提取支持的SECS/GEM功能
            var secsGemFeaturesStr = identification.GetValueOrDefault("SECS_GEM_FEATURES", "");
            var supportedSecsGemFeatures = !string.IsNullOrWhiteSpace(secsGemFeaturesStr)
                ? secsGemFeaturesStr.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(f => f.Trim()).ToList()
                : new List<string> { "S1F1", "S1F2", "S1F13", "S1F14", "S2F41", "S2F42", "S6F11", "S6F12" };

            return new DicingMachineCapabilities(
                maxWaferSize: maxWaferSize,
                supportedCuttingTypes: supportedCuttingTypes,
                maxCuttingSpeed: maxCuttingSpeed,
                accuracyLevel: accuracyLevel,
                supportedSecsGemFeatures: supportedSecsGemFeatures);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "提取设备能力失败，使用默认值");
            return DicingMachineCapabilities.Default();
        }
    }

    /// <summary>
    /// 提取扩展属性
    /// </summary>
    private Dictionary<string, string> ExtractExtendedProperties(Dictionary<string, string> identification)
    {
        var extendedProps = new Dictionary<string, string>();

        // 保存原始设备识别信息
        foreach (var kvp in identification)
        {
            extendedProps[$"DEVICE_{kvp.Key}"] = kvp.Value;
        }

        // 添加连接时间戳
        extendedProps["CONNECTION_TIMESTAMP"] = DateTime.UtcNow.ToString("O");

        // 添加管理器版本
        extendedProps["MANAGER_VERSION"] = typeof(MultiDicingMachineConnectionManager).Assembly.GetName().Version?.ToString() ?? "Unknown";

        return extendedProps;
    }

    /// <summary>
    /// 从字典中提取double值
    /// </summary>
    private double ExtractDoubleValue(Dictionary<string, string> dict, string key, double defaultValue)
    {
        if (dict.TryGetValue(key, out var value) && double.TryParse(value, out var parsedValue))
        {
            return parsedValue;
        }
        return defaultValue;
    }

    /// <summary>
    /// 验证裂片机元数据
    /// </summary>
    private void ValidateMachineMetadata(string machineNumber, string version)
    {
        // 验证编号唯一性
        var existingDevice = _dicingMachineMetadata.Values
            .FirstOrDefault(m => m.MachineNumber.Equals(machineNumber, StringComparison.OrdinalIgnoreCase));

        if (existingDevice != null)
        {
            throw new InvalidOperationException($"裂片机编号 {machineNumber} 已存在");
        }

        // 验证版本兼容性
        if (!IsVersionCompatible(version))
        {
            _logger.LogWarning("⚠️ 裂片机版本可能不兼容: {Version}", version);
        }
    }

    /// <summary>
    /// 检查版本兼容性
    /// </summary>
    private bool IsVersionCompatible(string version)
    {
        try
        {
            // 提取主版本号
            var versionMatch = System.Text.RegularExpressions.Regex.Match(version, @"V(\d+)\.(\d+)\.(\d+)");
            if (versionMatch.Success)
            {
                var major = int.Parse(versionMatch.Groups[1].Value);
                var minor = int.Parse(versionMatch.Groups[2].Value);

                // 支持版本范围: V1.x.x - V3.x.x
                return major >= 1 && major <= 3;
            }

            return true; // 无法解析时假设兼容
        }
        catch
        {
            return true; // 解析异常时假设兼容
        }
    }

    /// <summary>
    /// 处理连接状态变化事件
    /// </summary>
    private async Task HandleConnectionStateChangedAsync(EquipmentId equipmentId, ConnectionStateChangedEventArgs args)
    {
        try
        {
            var metadata = _dicingMachineMetadata.GetValueOrDefault(equipmentId);
            var machineNumber = metadata?.MachineNumber ?? equipmentId.Value;

            if (args.NewState.IsConnected && !args.PreviousState.IsConnected)
            {
                _logger.LogInformation("🟢 裂片机已连接: {MachineNumber} (会话: {SessionId})",
                    machineNumber, args.NewState.SessionId);

                // 发布连接成功事件
                await _mediator.Publish(new EquipmentConnectedEvent(
                    equipmentId,
                    args.NewState.ConnectedAt ?? DateTime.UtcNow,
                    $"{args.NewState.LastConnectedAt}",
                    args.NewState.SessionId ?? "Unknown",
                    args.PreviousState.IsConnected)); // 是否为重连
            }
            else if (!args.NewState.IsConnected && args.PreviousState.IsConnected)
            {
                _logger.LogWarning("🔴 裂片机已断开: {MachineNumber} (原因: {Reason})",
                    machineNumber, args.NewState.DisconnectReason ?? "未知");

                // 发布断开连接事件
                await _mediator.Publish(new EquipmentDisconnectedEvent(
                    equipmentId,
                    args.NewState.LastDisconnectedAt ?? DateTime.UtcNow,
                    args.NewState.DisconnectReason,
                    DetermineDisconnectionType(args.NewState.DisconnectReason),
                    args.PreviousState.SessionId,
                    args.PreviousState.ConnectionDuration,
                    EquipmentState.OFFLINE,
                    false, // 是否为预期断开
                    true)); // 是否需要关注
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理连接状态变化失败: {EquipmentId}", equipmentId);
        }
    }

    /// <summary>
    /// 处理数据接收事件
    /// </summary>
    private async Task HandleDataReceivedAsync(EquipmentId equipmentId, DeviceDataReceivedEventArgs args)
    {
        try
        {
            var metadata = _dicingMachineMetadata.GetValueOrDefault(equipmentId);
            var machineNumber = metadata?.MachineNumber ?? equipmentId.Value;

            _logger.LogDebug("📊 接收到裂片机数据: {MachineNumber} (数据项: {Count})",
                machineNumber, args.DataVariables.Count);

            // 这里可以添加裂片机特定的数据预处理逻辑
            // 例如：数据验证、异常值检测、趋势分析等
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理数据接收事件失败: {EquipmentId}", equipmentId);
        }
    }

    /// <summary>
    /// 处理报警事件
    /// </summary>
    private async Task HandleAlarmEventAsync(EquipmentId equipmentId, DeviceAlarmEventArgs args)
    {
        try
        {
            var metadata = _dicingMachineMetadata.GetValueOrDefault(equipmentId);
            var machineNumber = metadata?.MachineNumber ?? equipmentId.Value;

            var severityIcon = args.Severity switch
            {
                AlarmSeverity.CRITICAL => "🚨",
                AlarmSeverity.MAJOR => "⚠️",
                AlarmSeverity.MINOR => "💡",
                _ => "ℹ️"
            };

            _logger.LogWarning("{Icon} 裂片机报警: {MachineNumber} - {Message} (严重级别: {Severity})",
                severityIcon, machineNumber, args.Message, args.Severity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理报警事件失败: {EquipmentId}", equipmentId);
        }
    }

    /// <summary>
    /// 确定断开连接类型
    /// </summary>
    private DisconnectionType DetermineDisconnectionType(string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return DisconnectionType.Unexpected;

        var reasonLower = reason.ToLowerInvariant();

        if (reasonLower.Contains("timeout"))
            return DisconnectionType.Timeout;

        if (reasonLower.Contains("network") || reasonLower.Contains("connection"))
            return DisconnectionType.NetworkError;

        if (reasonLower.Contains("manual") || reasonLower.Contains("user"))
            return DisconnectionType.Manual;

        return DisconnectionType.Unexpected;
    }

    /// <summary>
    /// 对所有设备执行健康检查
    /// </summary>
    private async Task PerformHealthCheckOnAllDevicesAsync()
    {
        var healthTasks = _deviceServices.Values.Select(async deviceService =>
        {
            try
            {
                var healthResult = await deviceService.PerformHealthCheckAsync(_serviceCts.Token);

                if (healthResult.OverallStatus != HealthStatus.Healthy)
                {
                    var metadata = _dicingMachineMetadata.GetValueOrDefault(deviceService.EquipmentId);
                    _logger.LogWarning("⚠️ 设备健康检查异常: {MachineNumber} - 状态: {Status}",
                        metadata?.MachineNumber ?? deviceService.EquipmentId.Value, healthResult.OverallStatus);
                }

                return healthResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "设备健康检查失败: {EquipmentId}", deviceService.EquipmentId);
                return null;
            }
        });

        var results = await Task.WhenAll(healthTasks);
        var healthyCount = results.Count(r => r?.OverallStatus == HealthStatus.Healthy);
        var totalCount = results.Length;

        _logger.LogDebug("🏥 健康检查完成 [健康设备: {Healthy}/{Total}]", healthyCount, totalCount);
    }

    /// <summary>
    /// 监控连接状态
    /// </summary>
    private async Task MonitorConnectionsAsync()
    {
        var statistics = GetConnectionStatistics();

        if (statistics.ConnectionRate < 80.0) // 连接率低于80%时警告
        {
            _logger.LogWarning("⚠️ 裂片机连接率低: {Rate:F1}% ({Connected}/{Total})",
                statistics.ConnectionRate, statistics.ConnectedDevices, statistics.TotalDevices);
        }

        // 检查长时间未连接的设备
        var longDisconnectedDevices = await GetLongDisconnectedDevicesAsync();

        foreach (var device in longDisconnectedDevices)
        {
            _logger.LogWarning("⏰ 裂片机长时间断开: {MachineNumber} (断开时间: {Duration})",
                device.MachineNumber, DateTime.UtcNow - (device.LastHeartbeat ?? device.RegisteredAt));
        }
    }

    /// <summary>
    /// 获取长时间断开连接的设备
    /// </summary>
    private async Task<IEnumerable<DicingMachineStatus>> GetLongDisconnectedDevicesAsync()
    {
        var allStatuses = await GetAllDicingMachineStatusAsync();
        var threshold = TimeSpan.FromMinutes(10); // 10分钟阈值

        return allStatuses.Where(s =>
            !s.IsConnected &&
            (s.LastHeartbeat == null || DateTime.UtcNow - s.LastHeartbeat.Value > threshold));
    }

    /// <summary>
    /// 断开所有裂片机连接
    /// </summary>
    public async Task DisconnectAllDicingMachinesAsync()
    {
        _logger.LogInformation("🛑 断开所有裂片机连接...");

        var disconnectionTasks = _deviceServices.ToList().Select(async kvp =>
        {
            var equipmentId = kvp.Key;
            var deviceService = kvp.Value;
            var metadata = _dicingMachineMetadata.GetValueOrDefault(equipmentId);

            try
            {
                await deviceService.DisconnectAsync("系统关闭");
                await deviceService.StopAsync();

                _logger.LogInformation("✅ 裂片机已断开: {MachineNumber}",
                    metadata?.MachineNumber ?? equipmentId.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "断开裂片机失败: {MachineNumber}",
                    metadata?.MachineNumber ?? equipmentId.Value);
            }
        });

        await Task.WhenAll(disconnectionTasks);

        _deviceServices.Clear();
        _dicingMachineMetadata.Clear();
        _connectionTasks.Clear();

        _logger.LogInformation("✅ 所有裂片机连接已断开");
    }

    /// <summary>
    /// 获取指定裂片机服务
    /// </summary>
    public async Task<ISecsDeviceService?> GetDicingMachineServiceAsync(string machineNumber)
    {
        if (string.IsNullOrWhiteSpace(machineNumber))
        {
            throw new ArgumentException("裂片机编号不能为空", nameof(machineNumber));
        }

        // 标准化编号格式
        var normalizedNumber = machineNumber.PadLeft(3, '0');

        // 查找匹配的设备ID
        var matchingDevice = _dicingMachineMetadata.FirstOrDefault(kvp =>
            kvp.Value.MachineNumber.Equals(normalizedNumber, StringComparison.OrdinalIgnoreCase));

        if (matchingDevice.Key != null && _deviceServices.TryGetValue(matchingDevice.Key, out var deviceService))
        {
            return deviceService;
        }

        _logger.LogWarning("⚠️ 未找到裂片机服务: {MachineNumber}", machineNumber);
        return null;
    }

    /// <summary>
    /// 重连指定裂片机
    /// </summary>
    public async Task<bool> ReconnectDicingMachineAsync(string machineNumber)
    {
        try
        {
            var deviceService = await GetDicingMachineServiceAsync(machineNumber);

            if (deviceService == null)
            {
                _logger.LogError("❌ 重连失败: 未找到裂片机 {MachineNumber}", machineNumber);
                return false;
            }

            _logger.LogInformation("🔄 开始重连裂片机: {MachineNumber}", machineNumber);

            var success = await deviceService.ReconnectAsync(_serviceCts.Token);

            if (success)
            {
                _logger.LogInformation("✅ 裂片机重连成功: {MachineNumber}", machineNumber);
            }
            else
            {
                _logger.LogError("❌ 裂片机重连失败: {MachineNumber}", machineNumber);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "重连裂片机异常: {MachineNumber}", machineNumber);
            return false;
        }
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing && !_serviceCts.IsCancellationRequested)
        {
            _serviceCts.Cancel();
            _serviceCts.Dispose();
            _managementSemaphore.Dispose();
        }
    }

    #endregion
}
