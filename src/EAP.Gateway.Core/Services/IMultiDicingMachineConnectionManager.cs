using EAP.Gateway.Core.Models;
using EAP.Gateway.Core.Repositories;
using EAP.Gateway.Core.ValueObjects;

namespace EAP.Gateway.Core.Services;

/// <summary>
/// 多台裂片机连接管理器接口 (领域服务)
/// </summary>
public interface IMultiDicingMachineConnectionManager : IDisposable
{
    /// <summary>
    /// 添加并连接裂片机
    /// </summary>
    Task<DicingMachineConnectionResult> AddAndConnectDicingMachineAsync(
        string ipAddress,
        int port,
        string? expectedMachineNumber = null,
        TimeSpan? timeout = null);

    /// <summary>
    /// 并发连接多台裂片机
    /// </summary>
    Task<MultiConnectionResult> ConnectMultipleDicingMachinesAsync(
        IEnumerable<DicingMachineConfig> machineConfigs,
        int maxConcurrency = 5);

    /// <summary>
    /// 断开所有裂片机连接
    /// </summary>
    Task DisconnectAllDicingMachinesAsync();

    /// <summary>
    /// 获取指定裂片机服务
    /// </summary>
    Task<ISecsDeviceService?> GetDicingMachineServiceAsync(string machineNumber);

    /// <summary>
    /// 获取所有裂片机状态
    /// </summary>
    Task<IEnumerable<DicingMachineStatus>> GetAllDicingMachineStatusAsync();

    /// <summary>
    /// 获取连接统计信息
    /// </summary>
    ConnectionStatistics GetConnectionStatistics();

    /// <summary>
    /// 重连指定裂片机
    /// </summary>
    Task<bool> ReconnectDicingMachineAsync(string machineNumber);
}
