using System.ComponentModel.DataAnnotations;
using EAP.Gateway.Core.Common;
using System.Text.Json;

namespace EAP.Gateway.Core.Entities;

/// <summary>
/// 追踪数据实体（扩展版）
/// </summary>
public class TraceData : Entity<Guid>
{
    /// <summary>
    /// 设备ID
    /// </summary>
    [Required, MaxLength(50)]
    public string EquipmentId { get; set; } = string.Empty;

    /// <summary>
    /// 报告ID (SECS/GEM Report ID)
    /// </summary>
    public int ReportId { get; set; }

    /// <summary>
    /// 数据变量ID
    /// </summary>
    public uint VariableId { get; set; }

    /// <summary>
    /// 变量名称
    /// </summary>
    [MaxLength(200)]
    public string? VariableName { get; set; }

    /// <summary>
    /// 数据值（JSON格式存储）
    /// </summary>
    [Required, MaxLength(4000)]
    public string ValueJson { get; set; } = string.Empty;

    /// <summary>
    /// 数据值字典（用于批量数据）
    /// </summary>
    public IDictionary<string, object> DataValues { get; set; } = new Dictionary<string, object>();

    /// <summary>
    /// 数据类型
    /// </summary>
    [Required, MaxLength(50)]
    public string DataType { get; set; } = string.Empty;

    /// <summary>
    /// 数据单位
    /// </summary>
    [MaxLength(20)]
    public string? Unit { get; set; }

    /// <summary>
    /// 数据质量
    /// </summary>
    [MaxLength(20)]
    public string? Quality { get; set; } = "Good";

    /// <summary>
    /// 批次ID
    /// </summary>
    [MaxLength(100)]
    public string? LotId { get; set; }

    /// <summary>
    /// 载体ID
    /// </summary>
    [MaxLength(100)]
    public string? CarrierId { get; set; }

    /// <summary>
    /// 晶圆ID
    /// </summary>
    [MaxLength(100)]
    public string? WaferId { get; set; }

    /// <summary>
    /// 采集时间戳
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 数据来源
    /// </summary>
    [MaxLength(100)]
    public string? Source { get; set; }

    /// <summary>
    /// 是否为关键数据
    /// </summary>
    public bool IsCritical { get; set; }

    /// <summary>
    /// 数据项数量（计算属性）
    /// </summary>
    public int ItemCount => DataValues?.Count ?? 0;

    /// <summary>
    /// 解析后的值对象（从ValueJson解析）
    /// </summary>
    public object Value
    {
        get
        {
            if (string.IsNullOrEmpty(ValueJson))
                return new object();

            try
            {
                return JsonSerializer.Deserialize<object>(ValueJson) ?? new object();
            }
            catch
            {
                return ValueJson; // 如果解析失败，返回原始字符串
            }
        }
    }

    /// <summary>
    /// 接收时间（新增属性）
    /// </summary>
    public DateTime ReceivedAt { get; set; }

    /// <summary>
    /// 公共构造函数（EF Core需要）
    /// </summary>
    public TraceData() : base(Guid.NewGuid())
    {
        CreatedAt = DateTime.UtcNow;
        Timestamp = DateTime.UtcNow;
        ReceivedAt = DateTime.UtcNow;
    }


    /// <summary>
    /// 创建新的追踪数据
    /// </summary>
    public TraceData(
        string equipmentId,
        uint variableId,
        string valueJson,
        string dataType,
        DateTime? timestamp = null) : base(Guid.NewGuid())
    {
        EquipmentId = equipmentId ?? throw new ArgumentNullException(nameof(equipmentId));
        VariableId = variableId;
        ValueJson = valueJson ?? throw new ArgumentNullException(nameof(valueJson));
        DataType = dataType ?? throw new ArgumentNullException(nameof(dataType));
        Timestamp = timestamp ?? DateTime.UtcNow;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 创建追踪数据的静态工厂方法
    /// </summary>
    /// <param name="equipmentId">设备ID</param>
    /// <param name="reportId">报告ID</param>
    /// <param name="dataValues">数据值字典</param>
    /// <param name="timestamp">时间戳</param>
    /// <param name="lotId">批次ID</param>
    /// <param name="carrierId">载体ID</param>
    /// <returns>追踪数据实例</returns>
    public static TraceData Create(
        string equipmentId,
        int reportId,
        IDictionary<string, object> dataValues,
        DateTime? timestamp = null,
        string? lotId = null,
        string? carrierId = null)
    {
        if (string.IsNullOrWhiteSpace(equipmentId))
            throw new ArgumentException("Equipment ID cannot be null or empty", nameof(equipmentId));
        if (dataValues == null || !dataValues.Any())
            throw new ArgumentException("Data values cannot be null or empty", nameof(dataValues));

        var now = timestamp ?? DateTime.UtcNow;

        // 将数据值序列化为JSON
        var valueJson = JsonSerializer.Serialize(dataValues);

        return new TraceData
        {
            Id = Guid.NewGuid(),
            EquipmentId = equipmentId,
            ReportId = reportId,
            DataValues = new Dictionary<string, object>(dataValues),
            ValueJson = valueJson,
            DataType = "TraceData",
            Timestamp = now,
            CreatedAt = DateTime.UtcNow,
            LotId = lotId,
            CarrierId = carrierId,
            Source = "SECS/GEM"
        };
    }
}
