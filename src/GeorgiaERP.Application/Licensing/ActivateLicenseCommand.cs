using GeorgiaERP.Application.Common;
using MediatR;

namespace GeorgiaERP.Application.Licensing;

public record ActivateLicenseCommand(
    string LicenseKey,
    string CompanyName,
    string? ContactEmail) : IRequest<Result<LicenseActivationResponse>>;

public record LicenseActivationResponse(
    bool Activated,
    string? CompanyName,
    DateTimeOffset? ExpiresAt,
    int MaxUsers,
    int MaxStores);
