using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using EAP.Gateway.Core.Events.Common;
using EAP.Gateway.Core.ValueObjects;

namespace EAP.Gateway.Core.Events.Equipment;

/// <summary>
/// 设备配置变更事件
/// </summary>
public sealed class EquipmentConfigurationChangedEvent : DomainEventBase
{
    public EquipmentId EquipmentId { get; }
    public EquipmentConfiguration OldConfiguration { get; }
    public EquipmentConfiguration NewConfiguration { get; }
    public string? ChangedBy { get; }
    public DateTime ChangedAt { get; }
    public IReadOnlyList<string> Changes { get; }

    public EquipmentConfigurationChangedEvent(
        EquipmentId equipmentId,
        EquipmentConfiguration oldConfiguration,
        EquipmentConfiguration newConfiguration,
        string? changedBy,
        DateTime changedAt)
    {
        EquipmentId = equipmentId;
        OldConfiguration = oldConfiguration;
        NewConfiguration = newConfiguration;
        ChangedBy = changedBy;
        ChangedAt = changedAt;
        Changes = DetectChanges(oldConfiguration, newConfiguration);
    }

    private static IReadOnlyList<string> DetectChanges(EquipmentConfiguration oldConfig, EquipmentConfiguration newConfig)
    {
        var changes = new List<string>();

        if (!oldConfig.Endpoint.Equals(newConfig.Endpoint))
            changes.Add($"Endpoint: {oldConfig.Endpoint} → {newConfig.Endpoint}");

        // 修复：移除 DeviceId 比较，因为 EquipmentConfiguration 中没有该属性
        // if (oldConfig.DeviceId != newConfig.DeviceId)
        //     changes.Add($"DeviceId: {oldConfig.DeviceId} → {newConfig.DeviceId}");

        // 修复：使用正确的属性名称，根据 EquipmentConfiguration 的实际定义
        if (!oldConfig.Timeouts.Equals(newConfig.Timeouts))
            changes.Add($"Timeouts: {oldConfig.Timeouts} → {newConfig.Timeouts}");

        // 修复：T3TimeoutMs 不存在，使用 Timeouts.T3
        if (oldConfig.Timeouts.T3 != newConfig.Timeouts.T3)
            changes.Add($"T3Timeout: {oldConfig.Timeouts.T3} → {newConfig.Timeouts.T3}");

        if (oldConfig.EnableDataCollection != newConfig.EnableDataCollection)
            changes.Add($"DataCollection: {oldConfig.EnableDataCollection} → {newConfig.EnableDataCollection}");

        if (oldConfig.EnableRemoteControl != newConfig.EnableRemoteControl)
            changes.Add($"RemoteControl: {oldConfig.EnableRemoteControl} → {newConfig.EnableRemoteControl}");

        // 添加其他有用的配置变化检测
        if (oldConfig.ConnectionMode != newConfig.ConnectionMode)
            changes.Add($"ConnectionMode: {oldConfig.ConnectionMode} → {newConfig.ConnectionMode}");

        if (oldConfig.EnableAutoReconnect != newConfig.EnableAutoReconnect)
            changes.Add($"AutoReconnect: {oldConfig.EnableAutoReconnect} → {newConfig.EnableAutoReconnect}");

        if (oldConfig.HeartbeatInterval != newConfig.HeartbeatInterval)
            changes.Add($"HeartbeatInterval: {oldConfig.HeartbeatInterval} → {newConfig.HeartbeatInterval}");

        if (oldConfig.EnableAlarmHandling != newConfig.EnableAlarmHandling)
            changes.Add($"AlarmHandling: {oldConfig.EnableAlarmHandling} → {newConfig.EnableAlarmHandling}");

        return changes.AsReadOnly();
    }
}
