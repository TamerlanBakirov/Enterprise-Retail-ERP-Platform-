using FluentValidation;
using GeorgiaERP.Application.Compliance.Commands;

namespace GeorgiaERP.Application.Compliance.Validators;

public class CreateWaybillCommandValidator : AbstractValidator<CreateWaybillCommand>
{
    public CreateWaybillCommandValidator()
    {
        RuleFor(x => x.WaybillType)
            .GreaterThan(0).WithMessage("A valid RS.GE waybill type is required.");

        RuleFor(x => x.BuyerTin)
            .NotEmpty().WithMessage("Buyer TIN is required.")
            .MaximumLength(20);

        RuleFor(x => x.StartAddress)
            .NotEmpty().WithMessage("Start address is required.")
            .MaximumLength(500);

        RuleFor(x => x.EndAddress)
            .NotEmpty().WithMessage("End address is required.")
            .MaximumLength(500);

        RuleFor(x => x.Goods)
            .NotEmpty().WithMessage("A waybill must contain at least one goods line.");

        RuleForEach(x => x.Goods).ChildRules(goods =>
        {
            goods.RuleFor(g => g.ProductName)
                .NotEmpty().WithMessage("Goods line requires a product name.")
                .MaximumLength(300);

            goods.RuleFor(g => g.UnitId)
                .GreaterThan(0).WithMessage("Goods line requires a valid RS.GE unit id.");

            goods.RuleFor(g => g.Quantity)
                .GreaterThan(0).WithMessage("Goods quantity must be positive.");

            goods.RuleFor(g => g.Price)
                .GreaterThanOrEqualTo(0).WithMessage("Goods price cannot be negative.");
        });
    }
}
