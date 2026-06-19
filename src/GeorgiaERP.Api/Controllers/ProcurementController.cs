using GeorgiaERP.Application.Procurement.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GeorgiaERP.Api.Controllers;

[Authorize]
public class ProcurementController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public ProcurementController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("suppliers")]
    public async Task<IActionResult> GetSuppliers(
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetSuppliersQuery(search, isActive, page, pageSize));
        return Ok(result);
    }

    [HttpGet("purchase-orders")]
    public async Task<IActionResult> GetPurchaseOrders(
        [FromQuery] Guid? supplierId = null,
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetPurchaseOrdersQuery(supplierId, status, page, pageSize));
        return Ok(result);
    }
}
