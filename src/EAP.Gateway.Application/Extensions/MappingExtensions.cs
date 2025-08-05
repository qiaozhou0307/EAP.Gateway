using EAP.Gateway.Application.DTOs;
using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using EAP.Gateway.Core.Entities;
using EAP.Gateway.Core.Models;
using EAP.Gateway.Core.ValueObjects;

namespace EAP.Gateway.Application.Extensions;

/// <summary>
/// 映射扩展方法
/// </summary>
public static class MappingExtensions
{

    #region Equipment相关映射

    /// <summary>
    /// Equipment实体转EquipmentStatusDto
    /// </summary>
    /// <param name="equipment">设备实体</param>
    /// <returns>设备状态DTO</returns>
    public static EquipmentStatusDto ToStatusDto(this Equipment equipment)
    {
        return new EquipmentStatusDto
        {
            EquipmentId = equipment.Id.Value,
            Name = equipment.Name,
            State = equipment.State.ToString(),
            SubState = equipment.SubState,
            ConnectionState = GetConnectionStateString(equipment.ConnectionState),
            HealthStatus = equipment.HealthStatus.ToString(),
            LastHeartbeat = equipment.LastHeartbeat,
            LastDataUpdate = equipment.LastDataUpdate,
            UpdatedAt = equipment.UpdatedAt,
            ActiveAlarmsCount = equipment.ActiveAlarms?.Count ?? 0,
            Metrics = equipment.Metrics?.ToDto()
        };
    }

    /// <summary>
    /// Equipment实体转EquipmentDetailsDto
    /// </summary>
    /// <param name="equipment">设备实体</param>
    /// <param name="includeConfiguration">是否包含配置信息</param>
    /// <param name="includeMetrics">是否包含指标信息</param>
    /// <param name="includeAlarms">是否包含报警信息</param>
    /// <param name="includeCommands">是否包含命令信息</param>
    /// <returns>设备详细信息DTO</returns>
    public static EquipmentDetailsDto ToDetailsDto(this Equipment equipment,
        bool includeConfiguration = false,
        bool includeMetrics = false,
        bool includeAlarms = false,
        bool includeCommands = false)
    {
        var dto = new EquipmentDetailsDto
        {
            EquipmentId = equipment.Id.Value,
            Name = equipment.Name,
            Description = equipment.Description,
            State = equipment.State.ToString(),
            SubState = equipment.SubState,
            IsConnected = equipment.ConnectionState?.IsConnected ?? false,
            HealthStatus = equipment.HealthStatus.ToString(),
            LastHeartbeat = equipment.LastHeartbeat,
            LastDataUpdate = equipment.LastDataUpdate,
            CreatedAt = equipment.CreatedAt,
            UpdatedAt = equipment.UpdatedAt,
            CreatedBy = equipment.CreatedBy,
            UpdatedBy = equipment.UpdatedBy
        };

        if (includeConfiguration)
        {
            dto.Configuration = CreateConfigurationDto(equipment);
        }

        if (includeMetrics && equipment.Metrics != null)
        {
            dto.Metrics = equipment.Metrics.ToDto();
        }

        if (includeAlarms && equipment.ActiveAlarms?.Any() == true)
        {
            dto.ActiveAlarms = equipment.ActiveAlarms.Select(alarm => alarm.ToDto()).ToList();
        }

        if (includeCommands && equipment.CommandHistory?.Any() == true)
        {
            dto.RecentCommands = equipment.CommandHistory.Take(10).Select(cmd => cmd.ToDto()).ToList();
        }

        return dto;
    }

    /// <summary>
    /// 设备状态领域模型转DTO
    /// </summary>
    /// <param name="status">领域模型</param>
    /// <returns>DTO</returns>
    public static EquipmentStatusDto ToDto(this EquipmentStatus status)
    {
        return new EquipmentStatusDto
        {
            EquipmentId = status.EquipmentId.Value,
            Name = status.Name,
            State = status.State.ToString(),
            SubState = status.SubState,
            ConnectionState = status.ConnectionState.IsConnected ? "Connected" : "Disconnected",
            HealthStatus = status.HealthStatus.ToString(),
            LastHeartbeat = status.LastHeartbeat,
            LastDataUpdate = status.LastDataUpdate,
            UpdatedAt = status.UpdatedAt,
            ActiveAlarmsCount = status.ActiveAlarmsCount,
            Metrics = status.Metrics?.ToDto()
        };
    }

    /// <summary>
    /// 批量转换设备状态
    /// </summary>
    public static IEnumerable<EquipmentStatusDto> ToDto(this IEnumerable<EquipmentStatus> statuses)
    {
        return statuses.Select(s => s.ToDto());
    }

    /// <summary>
    /// 批量转换设备实体为状态DTO
    /// </summary>
    public static IEnumerable<EquipmentStatusDto> ToStatusDto(this IEnumerable<Equipment> equipments)
    {
        return equipments.Select(e => e.ToStatusDto());
    }

    #endregion

    #region DataVariables相关映射

    /// <summary>
    /// DataVariables领域模型转DataVariablesDto
    /// 这是修复编译错误的关键方法
    /// </summary>
    /// <param name="dataVariables">数据变量领域模型</param>
    /// <returns>数据变量DTO</returns>
    public static DataVariablesDto ToDto(this DataVariables dataVariables)
    {
        var variablesDict = new Dictionary<uint, DataVariableValueDto>();

        foreach (var (id, variable) in dataVariables.Variables)
        {
            variablesDict[id] = variable.ToDto();
        }

        return new DataVariablesDto
        {
            EquipmentId = dataVariables.EquipmentId.Value,
            Variables = variablesDict,
            LastUpdated = dataVariables.LastUpdated
        };
    }

    /// <summary>
    /// DataVariable领域模型转DataVariableValueDto
    /// </summary>
    /// <param name="dataVariable">数据变量领域模型</param>
    /// <returns>数据变量值DTO</returns>
    public static DataVariableValueDto ToDto(this DataVariable dataVariable)
    {
        return new DataVariableValueDto
        {
            Id = dataVariable.Id,
            Name = dataVariable.Name,
            Value = dataVariable.Value,
            DataType = dataVariable.DataType,
            Unit = dataVariable.Unit,
            Quality = dataVariable.Quality,
            Timestamp = dataVariable.Timestamp
        };
    }

    /// <summary>
    /// 批量转换DataVariable
    /// </summary>
    /// <param name="dataVariables">数据变量集合</param>
    /// <returns>数据变量值DTO集合</returns>
    public static IEnumerable<DataVariableValueDto> ToDto(this IEnumerable<DataVariable> dataVariables)
    {
        return dataVariables.Select(dv => dv.ToDto());
    }

    #endregion


    #region 其他实体映射

    /// <summary>
    /// 处理指标领域模型转DTO
    /// </summary>
    /// <param name="metrics">处理指标</param>
    /// <returns>DTO</returns>
    public static ProcessingMetricsDto ToDto(this ProcessingMetrics metrics)
    {
        return new ProcessingMetricsDto
        {
            // 基础指标
            TotalProcessed = metrics.TotalProcessed,
            SuccessCount = metrics.SuccessCount,
            FailureCount = metrics.FailureCount,
            SuccessRate = metrics.SuccessRate,
            LastResetAt = metrics.LastResetAt,

            // 兼容性属性
            TotalProcessedItems = metrics.TotalProcessed,
            TotalProcessingTime = TimeSpan.FromMilliseconds(metrics.AverageProcessingTime.TotalMilliseconds * metrics.TotalProcessed),
            AverageProcessingTime = metrics.AverageProcessingTime.TotalMilliseconds,
            ErrorCount = metrics.FailureCount,
            LastResetTime = metrics.LastResetAt
        };
    }

    /// <summary>
    /// AlarmEvent实体转DTO
    /// </summary>
    /// <param name="alarm">报警事件实体</param>
    /// <returns>报警事件DTO</returns>
    public static AlarmEventDto ToDto(this AlarmEvent alarm)
    {
        return new AlarmEventDto
        {
            AlarmId = alarm.AlarmId,
            AlarmCode = alarm.AlarmCode,
            AlarmText = alarm.AlarmText,
            Severity = alarm.Severity.ToString(),
            State = alarm.State.ToString(),
            SetTime = alarm.SetTime,
            ClearTime = alarm.ClearTime,
            IsAcknowledged = alarm.State == AlarmState.Acknowledged,
            AcknowledgedBy = alarm.AcknowledgedBy,
            AcknowledgedAt = alarm.AcknowledgedAt,
            IsActive = alarm.IsActive,
            DurationMs = alarm.Duration.TotalMilliseconds
        };
    }

    /// <summary>
    /// RemoteCommand实体转DTO
    /// </summary>
    /// <param name="command">远程命令实体</param>
    /// <returns>远程命令DTO</returns>
    public static RemoteCommandDto ToDto(this RemoteCommand command)
    {
        return new RemoteCommandDto
        {
            CommandId = command.Id,
            CommandName = command.CommandName,
            Status = command.Status.ToString(),
            RequestedAt = command.RequestedAt,
            CompletedAt = command.CompletedAt,
            RequestedBy = command.RequestedBy,
            ResultMessage = command.ResultMessage
        };
    }

    /// <summary>
    /// 连接状态值对象转DTO
    /// </summary>
    /// <param name="connectionState">连接状态</param>
    /// <returns>DTO</returns>
    public static ConnectionStateDto ToDto(this ConnectionState connectionState)
    {
        return new ConnectionStateDto
        {
            IsConnected = connectionState.IsConnected,
            Status = connectionState.IsConnected ? "Connected" : "Disconnected",
            LastConnectedAt = connectionState.LastConnectedAt,
            LastDisconnectedAt = connectionState.LastDisconnectedAt,
            DisconnectReason = connectionState.DisconnectReason,
            RetryCount = connectionState.RetryCount,
            SessionId = connectionState.SessionId,
            Quality = connectionState.Quality.ToString(),
            LastHeartbeatAt = connectionState.LastHeartbeatAt
        };
    }

    #endregion


    #region 私有辅助方法

    /// <summary>
    /// 获取连接状态字符串表示
    /// </summary>
    /// <param name="connectionState">连接状态值对象</param>
    /// <returns>连接状态字符串</returns>
    private static string GetConnectionStateString(ConnectionState? connectionState)
    {
        if (connectionState == null)
            return "Unknown";

        return connectionState.IsConnected ? "Connected" : "Disconnected";
    }

    /// <summary>
    /// 创建设备配置DTO
    /// </summary>
    /// <param name="equipment">设备实体</param>
    /// <returns>设备配置DTO</returns>
    private static EquipmentConfigurationDto CreateConfigurationDto(Equipment equipment)
    {
        return new EquipmentConfigurationDto
        {
            // 临时方案：从设备名称解析基础信息
            Manufacturer = ExtractManufacturerFromName(equipment.Name),
            Model = ExtractModelFromName(equipment.Name),
            SerialNumber = ExtractSerialNumberFromName(equipment.Name),

            // 从EquipmentConfiguration获取网络配置
            IpAddress = equipment.Configuration?.Endpoint?.IpAddress,
            Port = equipment.Configuration?.Endpoint?.Port,

            // 从EquipmentConfiguration获取功能配置
            EnableDataCollection = equipment.Configuration?.EnableDataCollection ?? false,
            DataCollectionInterval = equipment.Configuration?.HeartbeatInterval ?? 30,
            EnableAlarmCollection = equipment.Configuration?.EnableAlarmHandling ?? false
        };
    }

    /// <summary>
    /// 从设备名称提取制造商信息
    /// </summary>
    private static string? ExtractManufacturerFromName(string equipmentName)
    {
        if (string.IsNullOrWhiteSpace(equipmentName))
            return null;

        var parts = equipmentName.Split(new[] { '_', '-' }, StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 0 ? parts[0] : null;
    }

    /// <summary>
    /// 从设备名称提取型号信息
    /// </summary>
    private static string? ExtractModelFromName(string equipmentName)
    {
        if (string.IsNullOrWhiteSpace(equipmentName))
            return null;

        var parts = equipmentName.Split(new[] { '_', '-' }, StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 1 ? parts[1] : null;
    }

    /// <summary>
    /// 从设备名称提取序列号信息
    /// </summary>
    private static string? ExtractSerialNumberFromName(string equipmentName)
    {
        if (string.IsNullOrWhiteSpace(equipmentName))
            return null;

        var parts = equipmentName.Split(new[] { '_', '-' }, StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 2 ? parts[2] : null;
    }

    #endregion

}
