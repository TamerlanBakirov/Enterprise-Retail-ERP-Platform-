using FluentValidation;
using GeorgiaERP.Application.POS.Commands;

namespace GeorgiaERP.Application.POS.Validators;

public class VoidPosTransactionCommandValidator : AbstractValidator<VoidPosTransactionCommand>
{
    public VoidPosTransactionCommandValidator()
    {
        RuleFor(x => x.TransactionId).NotEmpty();
        RuleFor(x => x.VoidedBy).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}
