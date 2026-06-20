using FluentValidation;
using GeorgiaERP.Application.Inventory.Commands;

namespace GeorgiaERP.Application.Inventory.Validators;

public class AdjustStockCommandValidator : AbstractValidator<AdjustStockCommand>
{
    public AdjustStockCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.WarehouseId).NotEmpty();
        RuleFor(x => x.Quantity).NotEqual(0).WithMessage("Adjustment quantity must not be zero.");
        RuleFor(x => x.AdjustedBy).NotEmpty();
    }
}

public class CreateStockCountCommandValidator : AbstractValidator<CreateStockCountCommand>
{
    private static readonly string[] ValidCountTypes = ["Full", "Partial", "Cycle"];

    public CreateStockCountCommandValidator()
    {
        RuleFor(x => x.WarehouseId).NotEmpty();
        RuleFor(x => x.CountType).NotEmpty().Must(t => ValidCountTypes.Contains(t))
            .WithMessage("CountType must be one of: " + string.Join(", ", ValidCountTypes));
        RuleFor(x => x.CreatedBy).NotEmpty();
    }
}

public class CreateTransferOrderCommandValidator : AbstractValidator<CreateTransferOrderCommand>
{
    public CreateTransferOrderCommandValidator()
    {
        RuleFor(x => x.SourceWarehouseId).NotEmpty();
        RuleFor(x => x.DestWarehouseId).NotEmpty();
        RuleFor(x => x.SourceWarehouseId).NotEqual(x => x.DestWarehouseId)
            .WithMessage("Source and destination warehouses must be different.");
        RuleFor(x => x.RequestedBy).NotEmpty();
        RuleFor(x => x.Lines).NotEmpty().WithMessage("At least one transfer line is required.");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductId).NotEmpty();
            line.RuleFor(l => l.Quantity).GreaterThan(0);
        });
    }
}
