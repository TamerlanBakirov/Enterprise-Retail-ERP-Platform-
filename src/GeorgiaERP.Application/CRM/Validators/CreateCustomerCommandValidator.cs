using FluentValidation;
using GeorgiaERP.Application.CRM.Commands;

namespace GeorgiaERP.Application.CRM.Validators;

public class CreateCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerCommandValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.FirstNameKa).MaximumLength(100);
        RuleFor(x => x.LastNameKa).MaximumLength(100);
        RuleFor(x => x.Tin).Matches(@"^\d{9,11}$").When(x => !string.IsNullOrEmpty(x.Tin));
        RuleFor(x => x.Phone).MaximumLength(20);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrEmpty(x.Email));
    }
}
