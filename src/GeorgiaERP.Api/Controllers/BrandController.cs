using Microsoft.AspNetCore.Mvc;

namespace GeorgiaERP.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class BrandController : ControllerBase
{
    [HttpGet]
    public IActionResult GetBrandInfo()
    {
        return Ok(new
        {
            name = "Georgia ERP Platform",
            nameKa = "საქართველოს ERP პლატფორმა",
            version = "1.0.0",
            logo = new
            {
                svg = "/images/logo.svg",
                svgLight = "/images/logo-light.svg",
                png512 = "/images/logo-512.png",
                png192 = "/images/logo-192.png",
                png128 = "/images/logo-128.png",
                png64 = "/images/logo-64.png",
                png32 = "/images/logo-32.png",
                appleTouchIcon = "/images/apple-touch-icon.png",
                favicon = "/favicon.ico"
            }
        });
    }
}
