using FluentValidation;
using GeorgiaERP.Application.Products.Commands;

namespace GeorgiaERP.Application.Products.Validators;

public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Sku)
            .NotEmpty().WithMessage("SKU is required.")
            .MaximumLength(50)
            .Matches("^[A-Za-z0-9-]+$").WithMessage("SKU can only contain letters, numbers, and hyphens.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Product name is required.")
            .MaximumLength(300);

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Category is required.");

        RuleFor(x => x.UnitOfMeasure)
            .NotEmpty().WithMessage("Unit of measure is required.")
            .MaximumLength(20);

        RuleFor(x => x.WeightKg)
            .GreaterThan(0).When(x => x.WeightKg.HasValue)
            .WithMessage("Weight must be positive.");

        RuleFor(x => x.MinStockLevel)
            .GreaterThanOrEqualTo(0).When(x => x.MinStockLevel.HasValue);

        RuleFor(x => x.MaxStockLevel)
            .GreaterThan(x => x.MinStockLevel ?? 0).When(x => x.MaxStockLevel.HasValue)
            .WithMessage("Max stock level must be greater than min stock level.");

        RuleForEach(x => x.Barcodes).ChildRules(barcode =>
        {
            barcode.RuleFor(b => b.Barcode)
                .NotEmpty().WithMessage("Barcode value is required.")
                .MaximumLength(50);

            barcode.RuleFor(b => b.BarcodeType)
                .NotEmpty().WithMessage("Barcode type is required.");
        }).When(x => x.Barcodes is { Count: > 0 });
    }
}
