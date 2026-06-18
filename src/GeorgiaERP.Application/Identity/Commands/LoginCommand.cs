using GeorgiaERP.Application.Common;
using GeorgiaERP.Application.Identity.DTOs;
using MediatR;

namespace GeorgiaERP.Application.Identity.Commands;

public record LoginCommand(
    string Username,
    string Password,
    string? TwoFactorCode,
    string? IpAddress,
    string? DeviceInfo) : IRequest<Result<AuthResponse>>;
