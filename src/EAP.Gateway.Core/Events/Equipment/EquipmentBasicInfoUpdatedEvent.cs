using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using EAP.Gateway.Core.Events.Common;

namespace EAP.Gateway.Core.Events.Equipment;

/// <summary>
/// 设备基础信息更新事件
/// 当设备的制造商、型号、序列号等基础信息发生变化时发布
/// </summary>
public sealed class EquipmentBasicInfoUpdatedEvent : DomainEventBase
{
    /// <summary>
    /// 设备标识
    /// </summary>
    public EquipmentId EquipmentId { get; }

    /// <summary>
    /// 变更内容列表
    /// </summary>
    public IReadOnlyList<string> Changes { get; }

    /// <summary>
    /// 更新者
    /// </summary>
    public string? UpdatedBy { get; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; }

    /// <summary>
    /// 更新前的制造商信息
    /// </summary>
    public string? PreviousManufacturer { get; }

    /// <summary>
    /// 更新后的制造商信息
    /// </summary>
    public string? NewManufacturer { get; }

    /// <summary>
    /// 更新前的型号信息
    /// </summary>
    public string? PreviousModel { get; }

    /// <summary>
    /// 更新后的型号信息
    /// </summary>
    public string? NewModel { get; }

    /// <summary>
    /// 更新前的序列号
    /// </summary>
    public string? PreviousSerialNumber { get; }

    /// <summary>
    /// 更新后的序列号
    /// </summary>
    public string? NewSerialNumber { get; }

    /// <summary>
    /// 更新前的数据采集间隔
    /// </summary>
    public int? PreviousDataCollectionInterval { get; }

    /// <summary>
    /// 更新后的数据采集间隔
    /// </summary>
    public int? NewDataCollectionInterval { get; }

    /// <summary>
    /// 更新前的报警采集设置
    /// </summary>
    public bool PreviousEnableAlarmCollection { get; }

    /// <summary>
    /// 更新后的报警采集设置
    /// </summary>
    public bool NewEnableAlarmCollection { get; }

    /// <summary>
    /// 构造函数
    /// </summary>
    public EquipmentBasicInfoUpdatedEvent(
        EquipmentId equipmentId,
        IReadOnlyList<string> changes,
        string? updatedBy,
        DateTime updatedAt,
        string? previousManufacturer = null,
        string? newManufacturer = null,
        string? previousModel = null,
        string? newModel = null,
        string? previousSerialNumber = null,
        string? newSerialNumber = null,
        int? previousDataCollectionInterval = null,
        int? newDataCollectionInterval = null,
        bool previousEnableAlarmCollection = false,
        bool newEnableAlarmCollection = false)
    {
        EquipmentId = equipmentId ?? throw new ArgumentNullException(nameof(equipmentId));
        Changes = changes ?? throw new ArgumentNullException(nameof(changes));
        UpdatedBy = updatedBy;
        UpdatedAt = updatedAt;
        PreviousManufacturer = previousManufacturer;
        NewManufacturer = newManufacturer;
        PreviousModel = previousModel;
        NewModel = newModel;
        PreviousSerialNumber = previousSerialNumber;
        NewSerialNumber = newSerialNumber;
        PreviousDataCollectionInterval = previousDataCollectionInterval;
        NewDataCollectionInterval = newDataCollectionInterval;
        PreviousEnableAlarmCollection = previousEnableAlarmCollection;
        NewEnableAlarmCollection = newEnableAlarmCollection;
    }

    /// <summary>
    /// 简化构造函数 - 仅包含必要信息
    /// </summary>
    public EquipmentBasicInfoUpdatedEvent(
        EquipmentId equipmentId,
        IReadOnlyList<string> changes,
        string? updatedBy,
        DateTime updatedAt)
        : this(equipmentId, changes, updatedBy, updatedAt, null, null, null, null, null, null, null, null, false, false)
    {
    }

    /// <summary>
    /// 创建设备基础信息更新事件的工厂方法
    /// </summary>
    /// <param name="equipmentId">设备标识</param>
    /// <param name="changes">变更内容</param>
    /// <param name="updatedBy">更新者</param>
    /// <returns>设备基础信息更新事件</returns>
    public static EquipmentBasicInfoUpdatedEvent Create(
        EquipmentId equipmentId,
        IReadOnlyList<string> changes,
        string? updatedBy = null)
    {
        return new EquipmentBasicInfoUpdatedEvent(
            equipmentId,
            changes,
            updatedBy,
            DateTime.UtcNow);
    }

    /// <summary>
    /// 检查是否包含特定类型的变更
    /// </summary>
    /// <param name="changeType">变更类型关键字</param>
    /// <returns>是否包含该类型变更</returns>
    public bool HasChangeType(string changeType)
    {
        return Changes.Any(change => change.Contains(changeType, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 检查是否为关键信息变更（制造商或型号）
    /// </summary>
    public bool IsCriticalChange => HasChangeType("Manufacturer") || HasChangeType("Model");
}
