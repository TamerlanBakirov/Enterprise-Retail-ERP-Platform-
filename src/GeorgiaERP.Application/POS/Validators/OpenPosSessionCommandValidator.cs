using FluentValidation;
using GeorgiaERP.Application.POS.Commands;

namespace GeorgiaERP.Application.POS.Validators;

public class OpenPosSessionCommandValidator : AbstractValidator<OpenPosSessionCommand>
{
    public OpenPosSessionCommandValidator()
    {
        RuleFor(x => x.TerminalId).NotEmpty();
        RuleFor(x => x.CashierId).NotEmpty();
        RuleFor(x => x.OpeningBalance).GreaterThanOrEqualTo(0);
    }
}
