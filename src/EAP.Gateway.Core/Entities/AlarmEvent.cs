using System.ComponentModel.DataAnnotations;
using EAP.Gateway.Core.ValueObjects;
using EAP.Gateway.Core.Common;

namespace EAP.Gateway.Core.Entities;

/// <summary>
/// 报警事件实体（扩展版本）
/// </summary>
public class AlarmEvent : Entity<Guid>
{
    /// <summary>
    /// 设备ID
    /// </summary>
    [Required, MaxLength(50)]
    public string EquipmentId { get; set; } = string.Empty;

    /// <summary>
    /// 报警ID
    /// </summary>
    public uint AlarmId { get; set; }

    /// <summary>
    /// 报警代码
    /// </summary>
    [MaxLength(100)]
    public string? AlarmCode { get; set; }

    /// <summary>
    /// 报警文本描述
    /// </summary>
    [MaxLength(500)]
    public string? AlarmText { get; set; }

    /// <summary>
    /// 报警严重程度
    /// </summary>
    public AlarmSeverity Severity { get; set; }

    /// <summary>
    /// 报警状态
    /// </summary>
    public AlarmState State { get; set; }

    /// <summary>
    /// 报警设置时间
    /// </summary>
    public DateTime SetTime { get; set; }

    /// <summary>
    /// 报警清除时间
    /// </summary>
    public DateTime? ClearTime { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// 报警参数（JSON格式存储）
    /// </summary>
    [MaxLength(2000)]
    public string? Parameters { get; set; }

    /// <summary>
    /// 确认者
    /// </summary>
    [MaxLength(100)]
    public string? AcknowledgedBy { get; set; }

    /// <summary>
    /// 确认时间
    /// </summary>
    public DateTime? AcknowledgedAt { get; set; }

    /// <summary>
    /// 是否为设置状态
    /// </summary>
    public bool IsSet
    {
        get => State == AlarmState.Set;
        set => State = value ? AlarmState.Set : AlarmState.Cleared;
    }

    /// <summary>
    /// 时间戳（兼容属性，映射到 SetTime）
    /// </summary>
    public DateTime Timestamp
    {
        get => SetTime;
        set => SetTime = value;
    }

    /// <summary>
    /// 附加数据（字典格式）
    /// </summary>
    public IDictionary<string, object>? AdditionalData { get; set; }

    /// <summary>
    /// 清除时间（兼容属性，映射到 ClearTime）
    /// </summary>
    public DateTime? ClearedAt
    {
        get => ClearTime;
        set => ClearTime = value;
    }

    /// <summary>
    /// 清除原因
    /// </summary>
    [MaxLength(500)]
    public string? ClearReason { get; set; }

    /// <summary>
    /// 清除者
    /// </summary>
    [MaxLength(100)]
    public string? ClearedBy { get; set; }


    /// <summary>
    /// 公共构造函数（修复访问级别）
    /// </summary>
    public AlarmEvent() : base(Guid.NewGuid())
    {
        CreatedAt = DateTime.UtcNow;
        SetTime = DateTime.UtcNow;
    }

    /// <summary>
    /// 创建新的报警事件（带参数的构造函数）
    /// </summary>
    public AlarmEvent(
        string equipmentId,
        uint alarmId,
        string? alarmCode,
        string? alarmText,
        AlarmSeverity severity,
        DateTime? setTime = null,
        string? parameters = null) : base(Guid.NewGuid())
    {
        EquipmentId = equipmentId ?? throw new ArgumentNullException(nameof(equipmentId));
        AlarmId = alarmId;
        AlarmCode = alarmCode;
        AlarmText = alarmText;
        Severity = severity;
        State = AlarmState.Set;
        SetTime = setTime ?? DateTime.UtcNow;
        CreatedAt = DateTime.UtcNow;
        Parameters = parameters;
    }


    /// <summary>
    /// 创建报警副本（替代 with 表达式）
    /// </summary>
    public AlarmEvent Clone()
    {
        return new AlarmEvent
        {
            // 注意：不要设置 Id，让基类自动生成新的 ID
            EquipmentId = this.EquipmentId,
            AlarmId = this.AlarmId,
            AlarmCode = this.AlarmCode,
            AlarmText = this.AlarmText,
            Severity = this.Severity,
            State = this.State,
            SetTime = this.SetTime,
            ClearTime = this.ClearTime,
            CreatedAt = this.CreatedAt,
            UpdatedAt = this.UpdatedAt,
            Parameters = this.Parameters,
            AcknowledgedBy = this.AcknowledgedBy,
            AcknowledgedAt = this.AcknowledgedAt,
            AdditionalData = this.AdditionalData,
            ClearReason = this.ClearReason,
            ClearedBy = this.ClearedBy
        };
    }

    /// <summary>
    /// 创建清除状态的报警副本
    /// </summary>
    public AlarmEvent CreateClearedCopy(string? clearReason = null, string? clearedBy = null)
    {
        var cleared = Clone();
        cleared.IsSet = false;
        cleared.ClearedAt = DateTime.UtcNow;
        cleared.ClearReason = clearReason;
        cleared.ClearedBy = clearedBy;
        cleared.State = AlarmState.Cleared;
        AdditionalData = this.AdditionalData;
        return cleared;
    }

    /// <summary>
    /// 创建报警事件的静态工厂方法
    /// </summary>
    public static AlarmEvent Create(
        string equipmentId,
        ushort alarmId,
        string alarmText,
        AlarmSeverity severity,
        DateTime? timestamp = null,
        IDictionary<string, object>? additionalData = null)
    {
        var alarm = new AlarmEvent(equipmentId, alarmId, null, alarmText, severity, timestamp);
        alarm.AdditionalData = additionalData;
        return alarm;
    }

    /// <summary>
    /// 确认报警
    /// </summary>
    public void Acknowledge(string acknowledgedBy)
    {
        if (State == AlarmState.Acknowledged || State == AlarmState.Cleared)
            return;

        State = AlarmState.Acknowledged;
        AcknowledgedBy = acknowledgedBy;
        AcknowledgedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 清除报警
    /// </summary>
    public void Clear(string? clearedBy = null, string? clearReason = null)
    {
        if (State == AlarmState.Cleared)
            return;

        State = AlarmState.Cleared;
        ClearTime = DateTime.UtcNow;
        ClearedBy = clearedBy;
        ClearReason = clearReason;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 检查报警是否活跃
    /// </summary>
    public bool IsActive => State == AlarmState.Set || State == AlarmState.Acknowledged;

    /// <summary>
    /// 检查报警是否已清除
    /// </summary>
    public bool IsCleared => State == AlarmState.Cleared;

    /// <summary>
    /// 获取报警持续时间
    /// </summary>
    public TimeSpan Duration => (ClearTime ?? DateTime.UtcNow) - SetTime;


}
