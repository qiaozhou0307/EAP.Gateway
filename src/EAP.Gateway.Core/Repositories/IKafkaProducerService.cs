namespace EAP.Gateway.Core.Repositories;

/// <summary>
/// Kafka消息发布服务接口 - 扩展版本
/// </summary>
public interface IKafkaProducerService
{
    /// <summary>
    /// 发布追踪数据
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    /// <param name="topic">主题名称</param>
    /// <param name="key">消息键</param>
    /// <param name="data">数据对象</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>发布是否成功</returns>
    Task<bool> PublishTraceDataAsync<T>(string topic, string key, T data, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// 发布设备事件
    /// </summary>
    /// <param name="equipmentId">设备ID</param>
    /// <param name="eventType">事件类型</param>
    /// <param name="eventData">事件数据</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>发布是否成功</returns>
    Task<bool> PublishEquipmentEventAsync(string equipmentId, string eventType, object eventData,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 发布报警事件
    /// </summary>
    /// <param name="equipmentId">设备ID</param>
    /// <param name="alarmType">报警类型</param>
    /// <param name="alarmData">报警数据</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>发布是否成功</returns>
    Task<bool> PublishAlarmEventAsync(string equipmentId, string alarmType, object alarmData,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 发布设备状态
    /// </summary>
    /// <param name="equipmentId">设备ID</param>
    /// <param name="statusData">状态数据</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>发布是否成功</returns>
    Task<bool> PublishDeviceStatusAsync(string equipmentId, object statusData,
        CancellationToken cancellationToken = default);

    // Infrastructure层基础方法 - 返回bool表示成功/失败
    Task<bool> ProduceAsync<T>(string topic, T message, CancellationToken cancellationToken = default) where T : class;
    Task<bool> ProduceAsync<T>(string topic, string key, T message, CancellationToken cancellationToken = default) where T : class;
    Task FlushAsync(CancellationToken cancellationToken = default);

    // 状态检查
    bool IsConnected { get; }
}
