using FluentValidation;
using GeorgiaERP.Application.Identity.Commands;

namespace GeorgiaERP.Application.Identity.Validators;

public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}
