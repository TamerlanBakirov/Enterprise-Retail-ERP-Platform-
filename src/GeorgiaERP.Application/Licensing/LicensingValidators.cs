using FluentValidation;

namespace GeorgiaERP.Application.Licensing;

public class ActivateLicenseCommandValidator : AbstractValidator<ActivateLicenseCommand>
{
    public ActivateLicenseCommandValidator()
    {
        RuleFor(x => x.LicenseKey).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CompanyName).NotEmpty().MaximumLength(300);
        RuleFor(x => x.ContactEmail).EmailAddress().When(x => !string.IsNullOrEmpty(x.ContactEmail));
    }
}

public class RenewLicenseCommandValidator : AbstractValidator<RenewLicenseCommand>
{
    public RenewLicenseCommandValidator()
    {
        RuleFor(x => x.LicenseKey).NotEmpty().MaximumLength(200);
    }
}
