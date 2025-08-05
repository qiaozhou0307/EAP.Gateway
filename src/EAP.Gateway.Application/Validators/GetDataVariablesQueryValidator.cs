using EAP.Gateway.Application.Queries.Data;
using FluentValidation;

namespace EAP.Gateway.Application.Validators;

/// <summary>
/// 获取数据变量查询验证器
/// </summary>
public class GetDataVariablesQueryValidator : AbstractValidator<GetDataVariablesQuery>
{
    public GetDataVariablesQueryValidator()
    {
        RuleFor(x => x.EquipmentId)
            .NotNull()
            .WithMessage("设备ID不能为空");

        RuleFor(x => x.EquipmentId.Value)
            .NotEmpty()
            .WithMessage("设备ID值不能为空");

        When(x => x.VariableIds != null, () =>
        {
            RuleFor(x => x.VariableIds!)
                .Must(ids => ids.Length > 0)
                .WithMessage("如果指定变量ID，则不能为空数组")
                .Must(ids => ids.All(id => id > 0))
                .WithMessage("所有变量ID必须大于0")
                .Must(ids => ids.Distinct().Count() == ids.Length)
                .WithMessage("变量ID不能重复");
        });
    }
}
