using GeorgiaERP.Application.Backup;
using GeorgiaERP.Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GeorgiaERP.Api.Controllers;

/// <summary>
/// Database backup and restore operations. Admin-only.
/// Supports creating, listing, and deleting PostgreSQL database backups.
/// </summary>
[Authorize(Roles = "admin,super_admin")]
[Tags("Backup")]
[EnableRateLimiting("admin")]
public class BackupController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public BackupController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Creates a new database backup.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(BackupRecordDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateBackup([FromBody] CreateBackupRequest? request = null)
    {
        var userName = User.Identity?.Name;
        var command = new CreateBackupCommand(
            Type: request?.Type ?? BackupType.Full,
            Notes: request?.Notes,
            UserId: CurrentUserId,
            UserName: userName);

        var record = await _mediator.Send(command);

        var dto = new BackupRecordDto(
            record.Id,
            record.FileName,
            record.FileSizeBytes,
            record.Type.ToString(),
            record.Status.ToString(),
            record.ErrorMessage,
            record.StartedAt,
            record.CompletedAt,
            record.InitiatedByUserName,
            record.Notes);

        return Ok(dto);
    }

    /// <summary>
    /// Lists all backup records with pagination.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(BackupListResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListBackups([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new ListBackupsQuery(page, pageSize));
        return Ok(result);
    }

    /// <summary>
    /// Restores the database from a specific backup. Use with extreme caution.
    /// </summary>
    [HttpPost("{id:guid}/restore")]
    [Authorize(Roles = "super_admin")]
    [ProducesResponseType(typeof(RestoreBackupResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> RestoreBackup(Guid id)
    {
        var result = await _mediator.Send(new RestoreBackupCommand(id));
        if (!result.Success)
            return BadRequest(new { error = result.ErrorMessage });
        return Ok(result);
    }

    /// <summary>
    /// Deletes a backup record and its associated file.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteBackup(Guid id)
    {
        var deleted = await _mediator.Send(new DeleteBackupCommand(id));
        return deleted ? NoContent() : NotFound();
    }
}

public record CreateBackupRequest
{
    public BackupType Type { get; init; } = BackupType.Full;
    public string? Notes { get; init; }
}
