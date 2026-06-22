using GeorgiaERP.Application.Licensing;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GeorgiaERP.Api.Controllers;

/// <summary>
/// License management for desktop client validation and activation.
/// </summary>
[Tags("License")]
public class LicenseController : ApiControllerBase
{
    private readonly ILicenseValidator _licenseValidator;
    private readonly IMediator _mediator;

    public LicenseController(ILicenseValidator licenseValidator, IMediator mediator)
    {
        _licenseValidator = licenseValidator;
        _mediator = mediator;
    }

    [HttpGet("status")]
    [AllowAnonymous]
    public async Task<IActionResult> GetStatus()
    {
        var info = await _licenseValidator.ValidateAsync();
        return Ok(info);
    }

    [HttpPost("activate")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Activate([FromBody] ActivateLicenseCommand command)
    {
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPost("deactivate")]
    [Authorize(Roles = "super_admin,company_admin")]
    public async Task<IActionResult> Deactivate()
    {
        var result = await _mediator.Send(new DeactivateLicenseCommand());
        return ToActionResult(result);
    }

    [HttpPost("renew")]
    [Authorize(Roles = "super_admin,company_admin")]
    public async Task<IActionResult> Renew([FromBody] RenewLicenseCommand command)
    {
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }
}
