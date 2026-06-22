using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace GeorgiaERP.Api.Controllers;

/// <summary>
/// Application update checking for desktop client auto-update functionality.
/// </summary>
[Tags("Updates")]
public class UpdatesController : ApiControllerBase
{
    private readonly IConfiguration _configuration;

    public UpdatesController(IConfiguration configuration) => _configuration = configuration;

    [HttpGet("latest")]
    public IActionResult GetLatestVersion()
    {
        var version = _configuration["App:LatestVersion"] ?? "1.0.0";
        var downloadUrl = _configuration["App:DownloadUrl"];
        var releaseNotes = _configuration["App:ReleaseNotes"];
        var sha256 = _configuration["App:Sha256"];

        return Ok(new
        {
            version,
            downloadUrl,
            releaseNotes,
            sha256,
            fileSize = 0L
        });
    }
}
