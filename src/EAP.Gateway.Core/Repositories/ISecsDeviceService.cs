using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using EAP.Gateway.Core.ValueObjects;

namespace EAP.Gateway.Core.Repositories;

/// <summary>
/// SECS设备服务接口（领域服务接口）
/// </summary>
public interface ISecsDeviceService : IAsyncDisposable, IDisposable
{
    /// <summary>
    /// 设备标识
    /// </summary>
    EquipmentId EquipmentId { get; }

    /// <summary>
    /// 设备聚合根（只读）
    /// </summary>
    Equipment? Equipment { get; }

    /// <summary>
    /// 服务是否已启动
    /// </summary>
    bool IsStarted { get; }

    /// <summary>
    /// 服务是否已停止
    /// </summary>
    bool IsStopped { get; }

    /// <summary>
    /// 设备是否在线（连接且通信正常）
    /// </summary>
    bool IsOnline { get; }

    /// <summary>
    /// 设备健康状态
    /// </summary>
    HealthStatus HealthStatus { get; }

    /// <summary>
    /// 启动设备服务
    /// 初始化设备聚合根，建立通信连接，启动数据采集和监控
    /// </summary>
    /// <param name="equipment">设备聚合根实例</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>启动任务</returns>
    Task StartAsync(Equipment equipment, CancellationToken cancellationToken = default);

    /// <summary>
    /// 停止设备服务
    /// 断开通信连接，停止数据采集，保存设备状态
    /// </summary>
    /// <param name="reason">停止原因</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>停止任务</returns>
    Task StopAsync(string? reason = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 连接设备
    /// 建立HSMS连接并同步设备聚合根状态
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>连接是否成功</returns>
    Task<bool> ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 断开设备连接
    /// </summary>
    /// <param name="reason">断开原因</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>断开任务</returns>
    Task DisconnectAsync(string? reason = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 发送远程命令
    /// </summary>
    /// <param name="command">命令名称</param>
    /// <param name="parameters">命令参数</param>
    /// <param name="requestedBy">请求者</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>命令执行结果</returns>
    Task<RemoteCommandResult> SendRemoteCommandAsync(string command,
        IDictionary<string, object>? parameters = null,
        string? requestedBy = null,
        CancellationToken cancellationToken = default);
}
