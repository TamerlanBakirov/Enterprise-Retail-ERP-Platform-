using FluentValidation;
using GeorgiaERP.Application.Compliance.Commands;

namespace GeorgiaERP.Application.Compliance.Validators;

public class GenerateVatDeclarationCommandValidator : AbstractValidator<GenerateVatDeclarationCommand>
{
    public GenerateVatDeclarationCommandValidator()
    {
        RuleFor(x => x.Year)
            .InclusiveBetween(2000, 2100).WithMessage("Year must be a valid tax year.");

        RuleFor(x => x.Month)
            .InclusiveBetween(1, 12).WithMessage("Month must be between 1 and 12.");
    }
}
