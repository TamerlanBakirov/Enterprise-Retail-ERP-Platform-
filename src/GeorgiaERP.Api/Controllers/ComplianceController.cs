using GeorgiaERP.Application.Common;
using GeorgiaERP.Application.Compliance;
using GeorgiaERP.Application.Compliance.Commands;
using GeorgiaERP.Infrastructure.RsGe;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Api.Controllers;

[Authorize]
public class ComplianceController : ApiControllerBase
{
    private readonly IRsGeSoapClient _rsGeClient;
    private readonly IAppDbContext _dbContext;
    private readonly IMediator _mediator;

    public ComplianceController(IRsGeSoapClient rsGeClient, IAppDbContext dbContext, IMediator mediator)
    {
        _rsGeClient = rsGeClient;
        _dbContext = dbContext;
        _mediator = mediator;
    }

    [HttpGet("rsge/health")]
    public async Task<IActionResult> GetRsGeHealth()
    {
        try
        {
            var ip = await _rsGeClient.GetMyIpAsync();
            return Ok(new
            {
                Service = "RS.GE Integration",
                Status = "Connected",
                // SECURITY: Do not expose server IP or internal service URLs to clients.
                // These are logged server-side for troubleshooting.
                Timestamp = DateTimeOffset.UtcNow
            });
        }
        catch (Exception)
        {
            // SECURITY: Do not expose exception details to the client (OWASP A09:2021).
            return Ok(new
            {
                Service = "RS.GE Integration",
                Status = "Unavailable",
                Timestamp = DateTimeOffset.UtcNow
            });
        }
    }

    [HttpGet("rsge/units")]
    public async Task<IActionResult> GetRsGeUnits()
    {
        var units = await _rsGeClient.GetUnitsAsync();
        return Ok(units);
    }

    [HttpGet("rsge/transport-types")]
    public async Task<IActionResult> GetTransportTypes()
    {
        var types = await _rsGeClient.GetTransportTypesAsync();
        return Ok(types);
    }

    [HttpGet("rsge/waybill-types")]
    public async Task<IActionResult> GetWaybillTypes()
    {
        var types = await _rsGeClient.GetWaybillTypesAsync();
        return Ok(types);
    }

    [HttpGet("rsge/tin/{tin}/name")]
    public async Task<IActionResult> GetNameFromTin(string tin)
    {
        if (!IsValidTin(tin))
            return BadRequest(new { error = "TIN must be 9-11 digits." });

        var result = await _rsGeClient.GetNameFromTinAsync(tin);
        return Ok(result);
    }

    [HttpGet("rsge/tin/{tin}/vat-status")]
    public async Task<IActionResult> GetVatStatus(string tin)
    {
        if (!IsValidTin(tin))
            return BadRequest(new { error = "TIN must be 9-11 digits." });

        var isVatPayer = await _rsGeClient.IsVatPayerAsync(tin);
        return Ok(new { Tin = tin, IsVatPayer = isVatPayer });
    }

    /// <summary>
    /// Validates that a TIN is 9-11 digits only, preventing injection of
    /// arbitrary strings into RS.GE SOAP requests.
    /// </summary>
    private static bool IsValidTin(string tin) =>
        !string.IsNullOrWhiteSpace(tin) && System.Text.RegularExpressions.Regex.IsMatch(tin, @"^\d{9,11}$");

    [HttpPost("waybills")]
    public async Task<IActionResult> CreateWaybill([FromBody] CreateWaybillCommand command)
    {
        var result = await _mediator.Send(command);

        if (result.IsFailure)
            return ToActionResult(result);

        return Accepted(result.Value);
    }

    [HttpPost("waybills/{fiscalDocumentId:guid}/confirm")]
    public async Task<IActionResult> ConfirmWaybill(Guid fiscalDocumentId)
    {
        var result = await _mediator.Send(new EnqueueWaybillOperationCommand(fiscalDocumentId, RsGeOperation.ConfirmWaybill));
        return result.IsSuccess ? Accepted() : ToActionResult(result);
    }

    [HttpPost("waybills/{fiscalDocumentId:guid}/close")]
    public async Task<IActionResult> CloseWaybill(Guid fiscalDocumentId)
    {
        var result = await _mediator.Send(new EnqueueWaybillOperationCommand(fiscalDocumentId, RsGeOperation.CloseWaybill));
        return result.IsSuccess ? Accepted() : ToActionResult(result);
    }

    [HttpGet("waybills")]
    public async Task<IActionResult> GetWaybills(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var waybills = await _dbContext.RsGeWaybills
            .OrderByDescending(w => w.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(w => new
            {
                w.Id,
                w.FiscalDocumentId,
                w.WaybillNumber,
                w.WaybillType,
                w.SellerTin,
                w.BuyerTin,
                w.BuyerName,
                Status = w.Status.ToString(),
                w.TotalAmount,
                w.StartAddress,
                w.EndAddress,
                w.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(waybills);
    }

    [HttpGet("fiscal-documents")]
    public async Task<IActionResult> GetFiscalDocuments(
        [FromQuery] string? type = null,
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _dbContext.FiscalDocuments.AsQueryable();

        if (!string.IsNullOrEmpty(type) && Enum.TryParse<Domain.Compliance.FiscalDocumentType>(type, true, out var docType))
            query = query.Where(d => d.DocumentType == docType);

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<Domain.Compliance.FiscalDocumentStatus>(status, true, out var docStatus))
            query = query.Where(d => d.Status == docStatus);

        var documents = await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(d => new
            {
                d.Id,
                DocumentType = d.DocumentType.ToString(),
                d.DocumentNumber,
                d.InternalRef,
                Status = d.Status.ToString(),
                d.RsGeId,
                d.RsGeStatus,
                d.SubmissionDeadline,
                d.SubmittedAt,
                d.ConfirmedAt,
                d.RetryCount,
                d.LastError,
                d.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(documents);
    }

    [HttpGet("vat-summary")]
    public async Task<IActionResult> GetVatSummary(
        [FromQuery] int? year = null,
        [FromQuery] int? month = null,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        year ??= now.Year;
        month ??= now.Month;

        var periodStart = new DateTimeOffset(year.Value, month.Value, 1, 0, 0, 0, TimeSpan.Zero);
        var periodEnd = periodStart.AddMonths(1).AddDays(-1);

        var declaration = await _dbContext.VatDeclarations
            .Where(v => v.PeriodStart >= periodStart && v.PeriodEnd <= periodEnd)
            .FirstOrDefaultAsync(cancellationToken);

        return Ok(new
        {
            Period = $"{year:D4}-{month:D2}",
            OutputVat = declaration?.TotalOutputVat ?? 0m,
            InputVat = declaration?.TotalInputVat ?? 0m,
            NetVat = declaration?.NetVat ?? 0m,
            Status = declaration?.Status.ToString() ?? "NotFiled",
            Currency = "GEL"
        });
    }

    [HttpGet("deadlines")]
    public async Task<IActionResult> GetSubmissionDeadlines(
        [FromQuery] int warningDays = 7,
        CancellationToken cancellationToken = default)
    {
        warningDays = Math.Clamp(warningDays, 1, 30);
        var now = DateTimeOffset.UtcNow;
        var warningCutoff = now.AddDays(warningDays);
        var pendingStatuses = new[]
        {
            Domain.Compliance.FiscalDocumentStatus.Pending,
            Domain.Compliance.FiscalDocumentStatus.Queued,
            Domain.Compliance.FiscalDocumentStatus.Failed
        };

        var atRisk = await _dbContext.FiscalDocuments
            .Where(d => d.SubmissionDeadline != null && pendingStatuses.Contains(d.Status) &&
                        d.SubmissionDeadline <= warningCutoff)
            .OrderBy(d => d.SubmissionDeadline)
            .Select(d => new
            {
                d.Id,
                Type = d.DocumentType.ToString(),
                d.InternalRef,
                Status = d.Status.ToString(),
                Deadline = d.SubmissionDeadline,
                IsOverdue = d.SubmissionDeadline < now,
                d.LastError
            })
            .Take(200)
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            CheckedAt = now,
            OverdueCount = atRisk.Count(d => d.IsOverdue),
            DueSoonCount = atRisk.Count(d => !d.IsOverdue),
            Documents = atRisk
        });
    }
}
