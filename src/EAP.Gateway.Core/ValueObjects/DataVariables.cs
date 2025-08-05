using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using EAP.Gateway.Core.Common;

namespace EAP.Gateway.Core.ValueObjects;

/// <summary>
/// 数据变量集合值对象（领域模型）
/// </summary>
public class DataVariables : ValueObject
{
    public EquipmentId EquipmentId { get; }
    public IReadOnlyDictionary<uint, DataVariable> Variables { get; }
    public DateTime LastUpdated { get; }

    private DataVariables(EquipmentId equipmentId, IReadOnlyDictionary<uint, DataVariable> variables, DateTime lastUpdated)
    {
        EquipmentId = equipmentId;
        Variables = variables;
        LastUpdated = lastUpdated;
    }

    /// <summary>
    /// 创建空的数据变量集合
    /// </summary>
    public static DataVariables Empty(EquipmentId equipmentId)
    {
        return new DataVariables(
            equipmentId,
            new Dictionary<uint, DataVariable>(),
            DateTime.UtcNow);
    }

    /// <summary>
    /// 创建数据变量集合
    /// </summary>
    /// <param name="equipmentId">设备ID</param>
    /// <param name="variables">变量字典</param>
    /// <param name="lastUpdated">最后更新时间</param>
    /// <returns>数据变量集合</returns>
    public static DataVariables Create(
        EquipmentId equipmentId,
        IReadOnlyDictionary<uint, DataVariable> variables,
        DateTime? lastUpdated = null)
    {
        return new DataVariables(
            equipmentId,
            variables,
            lastUpdated ?? DateTime.UtcNow);
    }


    /// <summary>
    /// 更新单个变量
    /// </summary>
    public DataVariables UpdateVariable(uint variableId, object value, string? name = null)
    {
        var variableName = name ?? GetVariableName(variableId);
        var newVariable = new DataVariable(variableId, variableName, value, value.GetType().Name);

        var updatedVariables = new Dictionary<uint, DataVariable>(Variables)
        {
            [variableId] = newVariable
        };

        return new DataVariables(EquipmentId, updatedVariables, DateTime.UtcNow);
    }

    /// <summary>
    /// 批量更新变量
    /// </summary>
    public DataVariables UpdateVariables(IReadOnlyDictionary<uint, object> updates)
    {
        var updatedVariables = new Dictionary<uint, DataVariable>(Variables);
        var timestamp = DateTime.UtcNow;

        foreach (var (variableId, value) in updates)
        {
            var variableName = GetVariableName(variableId);
            updatedVariables[variableId] = new DataVariable(variableId, variableName, value, value.GetType().Name, timestamp: timestamp);
        }

        return new DataVariables(EquipmentId, updatedVariables, timestamp);
    }

    /// <summary>
    /// 获取变量名称
    /// </summary>
    private string GetVariableName(uint variableId)
    {
        if (Variables.TryGetValue(variableId, out var existing))
            return existing.Name;

        return $"DV_{variableId}";
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return EquipmentId;
        yield return LastUpdated;
        foreach (var variable in Variables.Values.OrderBy(v => v.Id))
        {
            yield return variable;
        }
    }
}
