using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GeorgiaERP.Api.Controllers;

[Authorize]
public class ComplianceController : ApiControllerBase
{
    [HttpGet("rsge/health")]
    public IActionResult GetRsGeHealth()
    {
        return Ok(new
        {
            Service = "RS.GE Integration",
            Status = "Configured",
            WaybillServiceUrl = "https://services.rs.ge/WayBillService/WayBillService.asmx",
            InvoiceServiceUrl = "https://webserv.rs.ge/specinvoices/SpecInvoicesService.asmx"
        });
    }

    [HttpGet("waybills")]
    public IActionResult GetWaybills()
    {
        return Ok(Array.Empty<object>());
    }

    [HttpGet("invoices")]
    public IActionResult GetInvoices()
    {
        return Ok(Array.Empty<object>());
    }

    [HttpGet("vat-summary")]
    public IActionResult GetVatSummary()
    {
        return Ok(new
        {
            Period = $"{DateOnly.FromDateTime(DateTime.UtcNow):yyyy-MM}",
            OutputVat = 0m,
            InputVat = 0m,
            NetVat = 0m,
            Currency = "GEL"
        });
    }
}
