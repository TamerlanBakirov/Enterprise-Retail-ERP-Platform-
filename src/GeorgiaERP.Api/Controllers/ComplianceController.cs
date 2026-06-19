using GeorgiaERP.Application.Common;
using GeorgiaERP.Infrastructure.RsGe;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Api.Controllers;

[Authorize]
public class ComplianceController : ApiControllerBase
{
    private readonly IRsGeSoapClient _rsGeClient;
    private readonly IAppDbContext _dbContext;

    public ComplianceController(IRsGeSoapClient rsGeClient, IAppDbContext dbContext)
    {
        _rsGeClient = rsGeClient;
        _dbContext = dbContext;
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
                ServerIp = ip,
                WaybillServiceUrl = "https://services.rs.ge/WayBillService/WayBillService.asmx",
                InvoiceServiceUrl = "https://webserv.rs.ge/specinvoices/SpecInvoicesService.asmx"
            });
        }
        catch (Exception ex)
        {
            return Ok(new
            {
                Service = "RS.GE Integration",
                Status = "Error",
                Error = ex.Message
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
        var result = await _rsGeClient.GetNameFromTinAsync(tin);
        return Ok(result);
    }

    [HttpGet("rsge/tin/{tin}/vat-status")]
    public async Task<IActionResult> GetVatStatus(string tin)
    {
        var isVatPayer = await _rsGeClient.IsVatPayerAsync(tin);
        return Ok(new { Tin = tin, IsVatPayer = isVatPayer });
    }

    [HttpGet("waybills")]
    public async Task<IActionResult> GetWaybills(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
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
            .ToListAsync();

        return Ok(waybills);
    }

    [HttpGet("fiscal-documents")]
    public async Task<IActionResult> GetFiscalDocuments(
        [FromQuery] string? type = null,
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
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
            .ToListAsync();

        return Ok(documents);
    }

    [HttpGet("vat-summary")]
    public async Task<IActionResult> GetVatSummary([FromQuery] int? year = null, [FromQuery] int? month = null)
    {
        var now = DateTime.UtcNow;
        year ??= now.Year;
        month ??= now.Month;

        var periodStart = new DateTimeOffset(year.Value, month.Value, 1, 0, 0, 0, TimeSpan.Zero);
        var periodEnd = periodStart.AddMonths(1).AddDays(-1);

        var declaration = await _dbContext.VatDeclarations
            .Where(v => v.PeriodStart >= periodStart && v.PeriodEnd <= periodEnd)
            .FirstOrDefaultAsync();

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
}
