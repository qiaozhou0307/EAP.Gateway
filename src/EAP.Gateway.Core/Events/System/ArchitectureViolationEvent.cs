using EAP.Gateway.Core.Events.Common;

namespace EAP.Gateway.Core.Events.System;

/// <summary>
/// 架构违规事件
/// </summary>
public sealed class ArchitectureViolationEvent : DomainEventBase
{
    public string ViolationType { get; }
    public string Description { get; }
    public string? SourceLocation { get; }
    public IDictionary<string, object>? Context { get; }
    public DateTime DetectedAt { get; }

    public ArchitectureViolationEvent(string violationType, string description, string? sourceLocation = null, IDictionary<string, object>? context = null)
    {
        ViolationType = violationType;
        Description = description;
        SourceLocation = sourceLocation;
        Context = context;
        DetectedAt = DateTime.UtcNow;
    }
}
