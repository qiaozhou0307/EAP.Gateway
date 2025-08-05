using EAP.Gateway.Core.Events.Common;

namespace EAP.Gateway.Core.Events.System;

/// <summary>
/// 系统配置变更事件
/// </summary>
public sealed class ConfigurationChangedEvent : DomainEventBase
{
    public string ConfigurationKey { get; }
    public object? OldValue { get; }
    public object? NewValue { get; }
    public string? ChangedBy { get; }
    public DateTime ChangedAt { get; }
    public string? Source { get; }

    public ConfigurationChangedEvent(string configurationKey, object? oldValue, object? newValue, string? changedBy = null, string? source = null)
    {
        ConfigurationKey = configurationKey;
        OldValue = oldValue;
        NewValue = newValue;
        ChangedBy = changedBy;
        ChangedAt = DateTime.UtcNow;
        Source = source;
    }
}
