using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using EAP.Gateway.Core.Entities;
using EAP.Gateway.Core.Events.Common;

namespace EAP.Gateway.Core.Events.Data;

/// <summary>
/// 追踪数据接收事件
/// 支持FR-BLL-005需求：实时数据处理(RTM/FMB)
/// </summary>
public class TraceDataReceivedEvent : DomainEventBase
{
    public EquipmentId EquipmentId { get; }
    public IReadOnlyDictionary<uint, object> DataVariables { get; }
    public DateTime ReceivedAt { get; }
    public string? LotId { get; }
    public string? CarrierId { get; }

    public TraceDataReceivedEvent(
        EquipmentId equipmentId,
        IReadOnlyDictionary<uint, object> dataVariables,
        DateTime receivedAt,
        string? lotId = null,
        string? carrierId = null)
    {
        EquipmentId = equipmentId ?? throw new ArgumentNullException(nameof(equipmentId));
        DataVariables = dataVariables ?? throw new ArgumentNullException(nameof(dataVariables));
        ReceivedAt = receivedAt;
        LotId = lotId;
        CarrierId = carrierId;
    }

    /// <summary>
    /// 便利构造函数 - 从单个TraceData创建事件
    /// 使用EquipmentId.Create工厂方法
    /// </summary>
    public static TraceDataReceivedEvent FromSingleTraceData(TraceData traceData)
    {
        ArgumentNullException.ThrowIfNull(traceData);

        var dataVariables = new Dictionary<uint, object>
        {
            [traceData.VariableId] = traceData.Value
        };

        return new TraceDataReceivedEvent(
            EquipmentId.Create(traceData.EquipmentId), // 使用Create工厂方法
            dataVariables,
            traceData.Timestamp,
            traceData.LotId,
            traceData.CarrierId);
    }

    /// <summary>
    /// 便利构造函数 - 从多个TraceData创建事件
    /// 使用EquipmentId.Create工厂方法
    /// </summary>
    public static TraceDataReceivedEvent FromMultipleTraceData(IEnumerable<TraceData> traceDataList)
    {
        if (traceDataList == null)
            throw new ArgumentNullException(nameof(traceDataList));

        var dataList = traceDataList.ToList();
        if (!dataList.Any())
            throw new ArgumentException("TraceData list cannot be empty", nameof(traceDataList));

        var firstItem = dataList.First();
        var dataVariables = dataList.ToDictionary(
            td => td.VariableId,
            td => (object)td.Value);

        return new TraceDataReceivedEvent(
            EquipmentId.Create(firstItem.EquipmentId), // 使用Create工厂方法
            dataVariables,
            firstItem.Timestamp,
            firstItem.LotId,
            firstItem.CarrierId);
    }

    /// <summary>
    /// 便利构造函数 - 直接从设备ID字符串和数据字典创建事件
    /// </summary>
    public static TraceDataReceivedEvent FromDataDictionary(
        string equipmentId,
        IReadOnlyDictionary<uint, object> dataVariables,
        DateTime? receivedAt = null,
        string? lotId = null,
        string? carrierId = null)
    {
        if (string.IsNullOrWhiteSpace(equipmentId))
            throw new ArgumentException("Equipment ID cannot be null or empty", nameof(equipmentId));

        if (dataVariables == null)
            throw new ArgumentNullException(nameof(dataVariables));

        return new TraceDataReceivedEvent(
            EquipmentId.Create(equipmentId), // 使用Create工厂方法
            dataVariables,
            receivedAt ?? DateTime.UtcNow,
            lotId,
            carrierId);
    }
}
