using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using EAP.Gateway.Core.ValueObjects;

namespace EAP.Gateway.Core.Exceptions;

/// <summary>
/// 设备未找到异常
/// </summary>
public class EquipmentNotFoundException : DomainException
{
    public EquipmentId EquipmentId { get; }

    public EquipmentNotFoundException(EquipmentId equipmentId)
        : base($"Equipment with ID '{equipmentId.Value}' was not found.")
    {
        EquipmentId = equipmentId;
    }

    public EquipmentNotFoundException(EquipmentId equipmentId, string message)
        : base(message)
    {
        EquipmentId = equipmentId;
    }

    public EquipmentNotFoundException(EquipmentId equipmentId, string message, Exception innerException)
        : base(message, innerException)
    {
        EquipmentId = equipmentId;
    }
}
