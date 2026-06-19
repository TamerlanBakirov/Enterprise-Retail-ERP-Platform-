using GeorgiaERP.Application.Licensing;
using Microsoft.AspNetCore.Mvc;

namespace GeorgiaERP.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class LicenseController : ControllerBase
{
    private readonly ILicenseValidator _licenseValidator;

    public LicenseController(ILicenseValidator licenseValidator) => _licenseValidator = licenseValidator;

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        var info = await _licenseValidator.ValidateAsync();
        return Ok(info);
    }
}
