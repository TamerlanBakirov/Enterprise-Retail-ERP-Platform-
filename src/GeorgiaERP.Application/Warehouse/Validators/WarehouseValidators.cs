using FluentValidation;
using GeorgiaERP.Application.Warehouse.Commands;

namespace GeorgiaERP.Application.Warehouse.Validators;

public class CreateWarehouseCommandValidator : AbstractValidator<CreateWarehouseCommand>
{
    public CreateWarehouseCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.NameKa).MaximumLength(200);
        RuleFor(x => x.WarehouseType).NotEmpty();
        RuleFor(x => x.Address).MaximumLength(500);
        RuleFor(x => x.City).MaximumLength(100);
        RuleFor(x => x.Region).MaximumLength(100);
    }
}

public class UpdateWarehouseCommandValidator : AbstractValidator<UpdateWarehouseCommand>
{
    public UpdateWarehouseCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.NameKa).MaximumLength(200);
        RuleFor(x => x.Address).MaximumLength(500);
        RuleFor(x => x.City).MaximumLength(100);
        RuleFor(x => x.Region).MaximumLength(100);
    }
}

public class CreateWarehouseLocationCommandValidator : AbstractValidator<CreateWarehouseLocationCommand>
{
    public CreateWarehouseLocationCommandValidator()
    {
        RuleFor(x => x.WarehouseId).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.NameKa).MaximumLength(200);
        RuleFor(x => x.LocationType).NotEmpty();
        RuleFor(x => x.MaxCapacity).GreaterThan(0).When(x => x.MaxCapacity.HasValue);
    }
}

public class CreateReceivingOrderCommandValidator : AbstractValidator<CreateReceivingOrderCommand>
{
    public CreateReceivingOrderCommandValidator()
    {
        RuleFor(x => x.WarehouseId).NotEmpty();
        RuleFor(x => x.Source).NotEmpty();
        RuleFor(x => x.Lines).NotEmpty().WithMessage("At least one line is required.");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductId).NotEmpty();
            line.RuleFor(l => l.ExpectedQty).GreaterThan(0);
        });
    }
}

public class ReceiveLineCommandValidator : AbstractValidator<ReceiveLineCommand>
{
    public ReceiveLineCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.LineId).NotEmpty();
        RuleFor(x => x.ReceivedQty).GreaterThan(0).LessThanOrEqualTo(999999);
        RuleFor(x => x.DamagedQty).GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(x => x.ReceivedQty).WithMessage("Damaged quantity cannot exceed received quantity.")
            .When(x => x.DamagedQty.HasValue);
    }
}

public class CreateShippingOrderCommandValidator : AbstractValidator<CreateShippingOrderCommand>
{
    public CreateShippingOrderCommandValidator()
    {
        RuleFor(x => x.WarehouseId).NotEmpty();
        RuleFor(x => x.OrderType).NotEmpty();
        RuleFor(x => x.ShippingAddress).MaximumLength(500);
        RuleFor(x => x.Carrier).MaximumLength(100);
        RuleFor(x => x.Lines).NotEmpty().WithMessage("At least one line is required.");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductId).NotEmpty();
            line.RuleFor(l => l.OrderedQty).GreaterThan(0);
        });
    }
}

public class PickLineCommandValidator : AbstractValidator<PickLineCommand>
{
    public PickLineCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.LineId).NotEmpty();
        RuleFor(x => x.PickedQty).GreaterThan(0).LessThanOrEqualTo(999999);
    }
}

public class ShipOrderCommandValidator : AbstractValidator<ShipOrderCommand>
{
    public ShipOrderCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ShippedBy).NotEmpty();
        RuleFor(x => x.TrackingNumber).MaximumLength(100);
    }
}
