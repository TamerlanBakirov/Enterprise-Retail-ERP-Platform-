using FluentValidation;
using GeorgiaERP.Application.POS.Commands;

namespace GeorgiaERP.Application.POS.Validators;

public class CreatePosTransactionCommandValidator : AbstractValidator<CreatePosTransactionCommand>
{
    public CreatePosTransactionCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();

        RuleFor(x => x.Lines).NotEmpty().WithMessage("At least one line item is required.");

        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.Quantity).GreaterThan(0);
            line.RuleFor(l => l.UnitPrice).GreaterThanOrEqualTo(0);
            line.RuleFor(l => l.DiscountAmount).GreaterThanOrEqualTo(0);
            line.RuleFor(l => l)
                .Must(l => l.ProductId.HasValue || !string.IsNullOrEmpty(l.Barcode))
                .WithMessage("Each line must have either ProductId or Barcode.");
        });

        RuleFor(x => x.Payments).NotEmpty().WithMessage("At least one payment is required.");

        RuleForEach(x => x.Payments).ChildRules(payment =>
        {
            payment.RuleFor(p => p.Amount).GreaterThan(0);
            payment.RuleFor(p => p.PaymentMethod).IsInEnum();
        });
    }
}
