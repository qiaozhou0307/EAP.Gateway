using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using EAP.Gateway.Core.ValueObjects;

namespace EAP.Gateway.Core.Events.Equipment;

/// <summary>
/// 设备数据接收事件参数
/// </summary>
public class DeviceDataReceivedEventArgs : EventArgs
{
    /// <summary>
    /// 设备ID
    /// </summary>
    public EquipmentId EquipmentId { get; }

    /// <summary>
    /// 接收到的数据
    /// </summary>
    public IReadOnlyDictionary<string, object> Data { get; }

    /// <summary>
    /// 数据类型
    /// </summary>
    public string DataType { get; }

    /// <summary>
    /// 接收时间
    /// </summary>
    public DateTime ReceivedAt { get; }

    /// <summary>
    /// 数据源
    /// </summary>
    public string? Source { get; }

    /// <summary>
    /// 批次ID
    /// </summary>
    public string? LotId { get; }

    /// <summary>
    /// 载体ID
    /// </summary>
    public string? CarrierId { get; }

    public DeviceDataReceivedEventArgs(
        EquipmentId equipmentId,
        IReadOnlyDictionary<string, object> data,
        string dataType,
        string? source = null,
        string? lotId = null,
        string? carrierId = null)
    {
        EquipmentId = equipmentId ?? throw new ArgumentNullException(nameof(equipmentId));
        Data = data ?? throw new ArgumentNullException(nameof(data));
        DataType = dataType ?? throw new ArgumentNullException(nameof(dataType));
        ReceivedAt = DateTime.UtcNow;
        Source = source;
        LotId = lotId;
        CarrierId = carrierId;
    }

    /// <summary>
    /// 获取特定类型的数据值
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    /// <param name="key">数据键</param>
    /// <returns>数据值</returns>
    public T? GetValue<T>(string key)
    {
        if (Data.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return default(T);
    }

    /// <summary>
    /// 检查是否包含指定键的数据
    /// </summary>
    /// <param name="key">数据键</param>
    /// <returns>是否包含</returns>
    public bool ContainsKey(string key)
    {
        return Data.ContainsKey(key);
    }
}
