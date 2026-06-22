using GeorgiaERP.Application.Identity.Commands;
using GeorgiaERP.Application.Identity.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GeorgiaERP.Api.Controllers;

/// <summary>
/// Authentication and authorization endpoints including login, token refresh,
/// logout, and two-factor authentication management.
/// </summary>
[Tags("Authentication")]
public class AuthController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Authenticates a user and returns JWT access and refresh tokens.
    /// </summary>
    /// <param name="request">Login credentials and optional 2FA code.</param>
    /// <returns>JWT tokens on success, 401 on failure.</returns>
    /// <response code="200">Authentication successful. Returns access and refresh tokens.</response>
    /// <response code="401">Invalid credentials or 2FA code.</response>
    /// <response code="429">Too many login attempts. Try again later.</response>
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var command = new LoginCommand(
            request.Username,
            request.Password,
            request.TwoFactorCode,
            CurrentIpAddress,
            Request.Headers.UserAgent.ToString());

        var result = await _mediator.Send(command);

        if (result.IsFailure)
            return Unauthorized(new { error = result.Error });

        return Ok(result.Value);
    }

    /// <summary>
    /// Exchanges a valid refresh token for a new access/refresh token pair.
    /// </summary>
    /// <response code="200">New tokens issued.</response>
    /// <response code="401">Invalid or expired refresh token.</response>
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost("refresh")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var command = new RefreshTokenCommand(request.RefreshToken, CurrentIpAddress);
        var result = await _mediator.Send(command);

        if (result.IsFailure)
            return Unauthorized(new { error = result.Error });

        return Ok(result.Value);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
    {
        var result = await _mediator.Send(new RevokeRefreshTokenCommand(request.RefreshToken, CurrentUserId));
        if (result.IsFailure)
            return ToActionResult(result);
        return Ok(new { message = "Logged out successfully" });
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        // SECURITY: Return only non-sensitive claim types. Never expose
        // raw token internals, permission lists, or internal IDs that
        // could aid privilege-escalation reconnaissance.
        var roles = User.Claims
            .Where(c => c.Type == "roles")
            .Select(c => c.Value)
            .ToList();

        return Ok(new
        {
            UserId = CurrentUserId,
            CompanyId = CurrentCompanyId,
            Username = User.FindFirst("username")?.Value,
            Email = User.FindFirst("email")?.Value,
            Roles = roles
        });
    }

    [Authorize]
    [HttpPost("2fa/setup")]
    public async Task<IActionResult> BeginTwoFactorSetup()
    {
        var result = await _mediator.Send(new BeginTwoFactorSetupCommand(CurrentUserId));
        return ToActionResult(result);
    }

    [Authorize]
    [HttpPost("2fa/confirm")]
    public async Task<IActionResult> ConfirmTwoFactorSetup([FromBody] TwoFactorCodeRequest request)
    {
        var result = await _mediator.Send(new ConfirmTwoFactorSetupCommand(CurrentUserId, request.Code));
        return ToActionResult(result);
    }

    [Authorize]
    [HttpPost("2fa/disable")]
    public async Task<IActionResult> DisableTwoFactor([FromBody] TwoFactorCodeRequest request)
    {
        var result = await _mediator.Send(new DisableTwoFactorCommand(CurrentUserId, request.Code));
        return ToActionResult(result);
    }

    public record TwoFactorCodeRequest(string Code);
}
