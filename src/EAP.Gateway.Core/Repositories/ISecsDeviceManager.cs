using EAP.Gateway.Core.Aggregates.EquipmentAggregate;

using EAP.Gateway.Core.Repositories;

/// <summary>
/// SECS设备管理器接口（领域服务接口）
/// 作为领域层的抽象，由基础设施层实现
/// </summary>
public interface ISecsDeviceManager : IDisposable
{
    /// <summary>
    /// 获取设备服务实例
    /// </summary>
    /// <param name="equipmentId">设备ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>设备服务实例</returns>
    Task<ISecsDeviceService?> GetDeviceServiceAsync(EquipmentId equipmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取所有设备服务实例
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>所有设备服务实例</returns>
    Task<IEnumerable<ISecsDeviceService>> GetAllDeviceServicesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 启动设备服务
    /// </summary>
    /// <param name="equipmentId">设备ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否启动成功</returns>
    Task<bool> StartDeviceServiceAsync(EquipmentId equipmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 停止设备服务
    /// </summary>
    /// <param name="equipmentId">设备ID</param>
    /// <param name="reason">停止原因</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否停止成功</returns>
    Task<bool> StopDeviceServiceAsync(EquipmentId equipmentId, string? reason = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查设备是否在线
    /// </summary>
    /// <param name="equipmentId">设备ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否在线</returns>
    Task<bool> IsDeviceOnlineAsync(EquipmentId equipmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取在线设备数量
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>在线设备数量</returns>
    Task<int> GetOnlineDeviceCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 重启设备服务
    /// </summary>
    /// <param name="equipmentId">设备ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否重启成功</returns>
    Task<bool> RestartDeviceServiceAsync(EquipmentId equipmentId, CancellationToken cancellationToken = default);
}
