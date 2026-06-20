using GeorgiaERP.Application.Identity.Commands;
using GeorgiaERP.Application.Identity.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GeorgiaERP.Api.Controllers;

public class AuthController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost("login")]
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

    [AllowAnonymous]
    [HttpPost("refresh")]
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
        var result = await _mediator.Send(new RevokeRefreshTokenCommand(request.RefreshToken));
        if (result.IsFailure) return BadRequest(new { error = result.Error });
        return Ok(new { message = "Logged out successfully" });
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
        return Ok(new
        {
            UserId = CurrentUserId,
            CompanyId = CurrentCompanyId,
            Claims = claims
        });
    }

    [Authorize]
    [HttpPost("2fa/setup")]
    public async Task<IActionResult> BeginTwoFactorSetup()
    {
        var result = await _mediator.Send(new BeginTwoFactorSetupCommand(CurrentUserId));
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [Authorize]
    [HttpPost("2fa/confirm")]
    public async Task<IActionResult> ConfirmTwoFactorSetup([FromBody] TwoFactorCodeRequest request)
    {
        var result = await _mediator.Send(new ConfirmTwoFactorSetupCommand(CurrentUserId, request.Code));
        return result.IsSuccess ? Ok() : BadRequest(new { error = result.Error });
    }

    [Authorize]
    [HttpPost("2fa/disable")]
    public async Task<IActionResult> DisableTwoFactor([FromBody] TwoFactorCodeRequest request)
    {
        var result = await _mediator.Send(new DisableTwoFactorCommand(CurrentUserId, request.Code));
        return result.IsSuccess ? Ok() : BadRequest(new { error = result.Error });
    }

    public record TwoFactorCodeRequest(string Code);
}
