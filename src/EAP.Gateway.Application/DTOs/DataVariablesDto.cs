namespace EAP.Gateway.Application.DTOs;

/// <summary>
/// 数据变量DTO
/// </summary>
public class DataVariablesDto
{
    /// <summary>
    /// 设备ID
    /// </summary>
    public required string EquipmentId { get; set; }

    /// <summary>
    /// 变量字典
    /// </summary>
    public required Dictionary<uint, DataVariableValueDto> Variables { get; set; }

    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTime LastUpdated { get; set; }

    /// <summary>
    /// 变量数量
    /// </summary>
    public int Count => Variables?.Count ?? 0;

    /// <summary>
    /// 构造函数
    /// </summary>
    public DataVariablesDto()
    {
        Variables = new Dictionary<uint, DataVariableValueDto>();
        LastUpdated = DateTime.UtcNow;
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="equipmentId">设备ID</param>
    /// <param name="variables">变量字典</param>
    /// <param name="lastUpdated">最后更新时间</param>
    public DataVariablesDto(string equipmentId, Dictionary<uint, DataVariableValueDto>? variables = null, DateTime? lastUpdated = null)
    {
        EquipmentId = equipmentId;
        Variables = variables ?? new Dictionary<uint, DataVariableValueDto>();
        LastUpdated = lastUpdated ?? DateTime.UtcNow;
    }

    /// <summary>
    /// 创建空的数据变量DTO
    /// </summary>
    /// <param name="equipmentId">设备ID</param>
    /// <returns>空的DataVariablesDto</returns>
    public static DataVariablesDto Empty(string equipmentId)
    {
        return new DataVariablesDto
        {
            EquipmentId = equipmentId,
            Variables = new Dictionary<uint, DataVariableValueDto>(),
            LastUpdated = DateTime.UtcNow
        };
    }

    /// <summary>
    /// 更新单个变量
    /// </summary>
    /// <param name="variableId">变量ID</param>
    /// <param name="value">变量值</param>
    /// <param name="name">变量名称</param>
    /// <param name="dataType">数据类型</param>
    /// <param name="unit">单位</param>
    /// <param name="quality">质量</param>
    public void UpdateVariable(uint variableId, object value, string? name = null,
        string? dataType = null, string? unit = null, string? quality = null)
    {
        Variables[variableId] = new DataVariableValueDto
        {
            Id = variableId,
            Name = name ?? $"DV_{variableId}",
            Value = value,
            DataType = dataType ?? value.GetType().Name,
            Unit = unit,
            Quality = quality ?? "Good",
            Timestamp = DateTime.UtcNow
        };

        LastUpdated = DateTime.UtcNow;
    }

    /// <summary>
    /// 批量更新变量
    /// </summary>
    /// <param name="updates">更新字典</param>
    public void UpdateVariables(IReadOnlyDictionary<uint, object> updates)
    {
        var timestamp = DateTime.UtcNow;

        foreach (var (variableId, value) in updates)
        {
            Variables[variableId] = new DataVariableValueDto
            {
                Id = variableId,
                Name = $"DV_{variableId}",
                Value = value,
                DataType = value.GetType().Name,
                Timestamp = timestamp,
                Quality = "Good"
            };
        }

        LastUpdated = timestamp;
    }

    /// <summary>
    /// 移除变量
    /// </summary>
    /// <param name="variableId">变量ID</param>
    /// <returns>是否移除成功</returns>
    public bool RemoveVariable(uint variableId)
    {
        var removed = Variables.Remove(variableId);
        if (removed)
        {
            LastUpdated = DateTime.UtcNow;
        }
        return removed;
    }

    /// <summary>
    /// 检查变量是否存在
    /// </summary>
    /// <param name="variableId">变量ID</param>
    /// <returns>是否存在</returns>
    public bool ContainsVariable(uint variableId)
    {
        return Variables.ContainsKey(variableId);
    }

    /// <summary>
    /// 获取变量值
    /// </summary>
    /// <param name="variableId">变量ID</param>
    /// <returns>变量值</returns>
    public object? GetVariableValue(uint variableId)
    {
        return Variables.TryGetValue(variableId, out var variable) ? variable.Value : null;
    }

    /// <summary>
    /// 清空所有变量
    /// </summary>
    public void Clear()
    {
        Variables.Clear();
        LastUpdated = DateTime.UtcNow;
    }
}
