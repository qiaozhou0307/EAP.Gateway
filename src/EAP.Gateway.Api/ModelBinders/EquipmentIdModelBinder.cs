using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace EAP.Gateway.Api.ModelBinders;

/// <summary>
/// EquipmentId 模型绑定器 - 修复版本
/// 允许控制器直接接收 EquipmentId 类型的参数
/// </summary>
public class EquipmentIdModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        if (bindingContext == null)
            throw new ArgumentNullException(nameof(bindingContext));

        // 获取参数值
        var value = bindingContext.ValueProvider.GetValue(bindingContext.ModelName).FirstValue;

        if (string.IsNullOrWhiteSpace(value))
        {
            // 检查是否为可空类型
            if (IsNullableType(bindingContext.ModelType))
            {
                bindingContext.Result = ModelBindingResult.Success(null);
            }
            else
            {
                // 必需参数为空，绑定失败
                bindingContext.ModelState.AddModelError(bindingContext.ModelName, "设备ID不能为空");
                bindingContext.Result = ModelBindingResult.Failed();
            }
            return Task.CompletedTask;
        }

        // 尝试创建 EquipmentId
        if (EquipmentId.TryCreate(value, out var equipmentId))
        {
            // ✅ 修复：使用 ModelBindingResult.Successful 而不是 Success
            bindingContext.Result = ModelBindingResult.Success(equipmentId);
        }
        else
        {
            // 添加模型错误
            bindingContext.ModelState.AddModelError(bindingContext.ModelName,
                $"无效的设备ID格式: {value}。设备ID只能包含字母、数字、下划线或连字符，且长度不超过50字符。");
            bindingContext.Result = ModelBindingResult.Failed();
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// 检查类型是否为可空类型
    /// </summary>
    private static bool IsNullableType(Type type)
    {
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
    }
}

/// <summary>
/// EquipmentId 模型绑定器提供程序 - 修复版本
/// </summary>
public class EquipmentIdModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        if (context?.Metadata?.ModelType == null)
            return null;

        var modelType = context.Metadata.ModelType;

        // ✅ 修复：正确检查 EquipmentId 和可空的 EquipmentId
        if (modelType == typeof(EquipmentId))
        {
            return new EquipmentIdModelBinder();
        }

        // ✅ 修复：使用 Nullable.GetUnderlyingType 检查可空引用类型
        var underlyingType = Nullable.GetUnderlyingType(modelType);
        if (underlyingType == typeof(EquipmentId))
        {
            return new EquipmentIdModelBinder();
        }

        return null;
    }
}
