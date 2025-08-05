using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using Microsoft.AspNetCore.Mvc;

namespace EAP.Gateway.Api.Extensions;

/// <summary>
/// 控制器扩展方法
/// </summary>
public static class ControllerExtensions
{
    /// <summary>
    /// 安全地解析设备ID，提供统一的错误处理
    /// </summary>
    /// <param name="controller">控制器实例</param>
    /// <param name="equipmentIdString">设备ID字符串</param>
    /// <returns>成功时返回 EquipmentId，失败时返回错误响应</returns>
    public static (EquipmentId? EquipmentId, IActionResult? ErrorResult) TryParseEquipmentId(
        this ControllerBase controller,
        string equipmentIdString)
    {
        if (string.IsNullOrWhiteSpace(equipmentIdString))
        {
            return (null, controller.BadRequest(new { Error = "设备ID不能为空" }));
        }

        if (!EquipmentId.TryCreate(equipmentIdString, out var equipmentId))
        {
            return (null, controller.BadRequest(new
            {
                Error = "无效的设备ID格式",
                ProvidedId = equipmentIdString,
                ExpectedFormat = "字母、数字、下划线或连字符，最长50字符"
            }));
        }

        return (equipmentId, null);
    }

    /// <summary>
    /// 批量解析设备ID列表
    /// </summary>
    /// <param name="controller">控制器实例</param>
    /// <param name="equipmentIdStrings">设备ID字符串列表</param>
    /// <returns>成功时返回 EquipmentId 列表，失败时返回错误响应</returns>
    public static (List<EquipmentId>? EquipmentIds, IActionResult? ErrorResult) TryParseEquipmentIds(
        this ControllerBase controller,
        string[] equipmentIdStrings)
    {
        var equipmentIds = new List<EquipmentId>();
        var invalidIds = new List<string>();

        foreach (var idString in equipmentIdStrings)
        {
            if (!EquipmentId.TryCreate(idString, out var equipmentId))
            {
                invalidIds.Add(idString);
            }
            else
            {
                equipmentIds.Add(equipmentId!);
            }
        }

        if (invalidIds.Count > 0)
        {
            return (null, controller.BadRequest(new
            {
                Error = "部分设备ID格式无效",
                InvalidIds = invalidIds
            }));
        }

        return (equipmentIds, null);
    }
}
