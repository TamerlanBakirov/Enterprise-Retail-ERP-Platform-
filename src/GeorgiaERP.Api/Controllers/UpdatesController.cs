using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace GeorgiaERP.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class UpdatesController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public UpdatesController(IConfiguration configuration) => _configuration = configuration;

    [HttpGet("latest")]
    public IActionResult GetLatestVersion()
    {
        var version = _configuration["App:LatestVersion"] ?? "1.0.0";
        var downloadUrl = _configuration["App:DownloadUrl"];
        var releaseNotes = _configuration["App:ReleaseNotes"];

        return Ok(new
        {
            version,
            downloadUrl,
            releaseNotes,
            fileSize = 0L
        });
    }
}
