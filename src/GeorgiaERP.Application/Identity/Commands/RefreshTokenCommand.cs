using GeorgiaERP.Application.Common;
using GeorgiaERP.Application.Identity.DTOs;
using MediatR;

namespace GeorgiaERP.Application.Identity.Commands;

public record RefreshTokenCommand(
    string RefreshToken,
    string? IpAddress) : IRequest<Result<AuthResponse>>;
