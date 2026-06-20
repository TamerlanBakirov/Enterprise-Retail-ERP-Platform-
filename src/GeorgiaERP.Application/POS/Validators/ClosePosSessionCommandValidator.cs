using FluentValidation;
using GeorgiaERP.Application.POS.Commands;

namespace GeorgiaERP.Application.POS.Validators;

public class ClosePosSessionCommandValidator : AbstractValidator<ClosePosSessionCommand>
{
    public ClosePosSessionCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.ClosingBalance).GreaterThanOrEqualTo(0);
    }
}
