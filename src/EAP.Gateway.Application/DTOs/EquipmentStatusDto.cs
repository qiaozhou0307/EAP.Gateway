using EAP.Gateway.Application.DTOs;

/// <summary>
/// 设备状态DTO - 统一版本，包含所有必要属性
/// 用于API数据传输和前端显示
/// </summary>
public class EquipmentStatusDto
{
    /// <summary>
    /// 设备ID
    /// </summary>
    public required string EquipmentId { get; set; }

    /// <summary>
    /// 设备名称
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// 设备状态
    /// </summary>
    public required string State { get; set; }

    /// <summary>
    /// 子状态描述
    /// </summary>
    public string? SubState { get; set; }

    /// <summary>
    /// 连接状态字符串
    /// </summary>
    public required string ConnectionState { get; set; }

    /// <summary>
    /// 是否已连接 - 便利属性，基于ConnectionState计算
    /// </summary>
    public bool IsConnected => ConnectionState == "Connected";

    /// <summary>
    /// 健康状态
    /// </summary>
    public required string HealthStatus { get; set; }

    /// <summary>
    /// 最后心跳时间
    /// </summary>
    public DateTime? LastHeartbeat { get; set; }

    /// <summary>
    /// 最后数据更新时间
    /// </summary>
    public DateTime? LastDataUpdate { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// 处理指标
    /// </summary>
    public ProcessingMetricsDto? Metrics { get; set; }

    /// <summary>
    /// 活动报警数量 - 修复：统一使用ActiveAlarmsCount
    /// </summary>
    public int ActiveAlarmsCount { get; set; }

    /// <summary>
    /// 是否在线
    /// </summary>
    public bool IsOnline => ConnectionState == "Connected" && HealthStatus != "Unhealthy";

    /// <summary>
    /// 是否需要关注（有报警或故障）
    /// </summary>
    public bool RequiresAttention => ActiveAlarmsCount > 0 ||
                                   State == "FAULT" ||
                                   State == "ALARM" ||
                                   HealthStatus == "Unhealthy";

    /// <summary>
    /// 状态显示颜色（用于前端UI）
    /// </summary>
    public string StatusColor => State switch
    {
        "EXECUTING" => "green",
        "IDLE" => "blue",
        "SETUP" => "orange",
        "PAUSE" => "yellow",
        "FAULT" => "red",
        "ALARM" => "red",
        "MAINTENANCE" => "purple",
        _ => "gray"
    };

    /// <summary>
    /// 连接状态显示文本
    /// </summary>
    public string ConnectionStatusDisplay => ConnectionState switch
    {
        "Connected" => "已连接",
        "Connecting" => "连接中",
        "Disconnected" => "已断开",
        "Failed" => "连接失败",
        _ => "未知"
    };

    /// <summary>
    /// 设备状态显示文本
    /// </summary>
    public string StateDisplay => State switch
    {
        "IDLE" => "空闲",
        "EXECUTING" => "执行中",
        "SETUP" => "设置中",
        "PAUSE" => "暂停",
        "FAULT" => "故障",
        "ALARM" => "报警",
        "MAINTENANCE" => "维护中",
        _ => State
    };

    /// <summary>
    /// 运行时长（从最后心跳算起）
    /// </summary>
    public TimeSpan? UpTime => LastHeartbeat.HasValue ? DateTime.UtcNow - LastHeartbeat.Value : null;

    /// <summary>
    /// 无参构造函数
    /// </summary>
    public EquipmentStatusDto()
    {
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 带参构造函数
    /// </summary>
    public EquipmentStatusDto(
        string equipmentId,
        string name,
        string state,
        string connectionState,
        string healthStatus)
    {
        EquipmentId = equipmentId;
        Name = name;
        State = state;
        ConnectionState = connectionState;
        HealthStatus = healthStatus;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 创建离线状态的设备DTO
    /// </summary>
    /// <param name="equipmentId">设备ID</param>
    /// <param name="name">设备名称</param>
    /// <returns>离线状态的DTO</returns>
    public static EquipmentStatusDto CreateOffline(string equipmentId, string name)
    {
        return new EquipmentStatusDto
        {
            EquipmentId = equipmentId,
            Name = name,
            State = "OFFLINE",
            ConnectionState = "Disconnected",
            HealthStatus = "Unknown",
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// 创建默认状态的设备DTO
    /// </summary>
    /// <param name="equipmentId">设备ID</param>
    /// <param name="name">设备名称</param>
    /// <returns>默认状态的DTO</returns>
    public static EquipmentStatusDto CreateDefault(string equipmentId, string name)
    {
        return new EquipmentStatusDto
        {
            EquipmentId = equipmentId,
            Name = name,
            State = "IDLE",
            ConnectionState = "Disconnected",
            HealthStatus = "Unknown",
            UpdatedAt = DateTime.UtcNow
        };
    }
}
