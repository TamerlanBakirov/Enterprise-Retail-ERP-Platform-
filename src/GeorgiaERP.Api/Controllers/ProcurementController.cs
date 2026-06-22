using GeorgiaERP.Application.Procurement.Commands;
using GeorgiaERP.Application.Procurement.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GeorgiaERP.Api.Controllers;

/// <summary>
/// Procurement management including purchase orders and goods receipt workflows.
/// </summary>
[Authorize]
[Tags("Procurement")]
public class ProcurementController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public ProcurementController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // Suppliers

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

    [HttpPost("suppliers")]
    public async Task<IActionResult> CreateSupplier([FromBody] CreateSupplierCommand command)
    {
        var result = await _mediator.Send(command);
        if (result.IsFailure)
            return ToActionResult(result);
        return Created($"/api/v1/procurement/suppliers/{result.Value}", new { id = result.Value });
    }

    // Purchase Orders

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

    [HttpPost("purchase-orders")]
    public async Task<IActionResult> CreatePurchaseOrder([FromBody] CreatePurchaseOrderCommand command)
    {
        var result = await _mediator.Send(command);
        if (result.IsFailure)
            return ToActionResult(result);
        return Created($"/api/v1/procurement/purchase-orders/{result.Value!.Id}", result.Value);
    }

    [HttpPost("purchase-orders/{id:guid}/approve")]
    public async Task<IActionResult> ApprovePurchaseOrder(Guid id)
    {
        var result = await _mediator.Send(new ApprovePurchaseOrderCommand(id, CurrentUserId));
        return ToActionResult(result);
    }

    [HttpPost("purchase-orders/{id:guid}/send")]
    public async Task<IActionResult> SendPurchaseOrder(Guid id)
    {
        var result = await _mediator.Send(new SendPurchaseOrderCommand(id));
        return ToActionResult(result);
    }

    [HttpPost("purchase-orders/{id:guid}/cancel")]
    public async Task<IActionResult> CancelPurchaseOrder(Guid id)
    {
        var result = await _mediator.Send(new CancelPurchaseOrderCommand(id));
        return ToActionResult(result);
    }

    // Goods Receipt

    [HttpPost("purchase-orders/{id:guid}/receive")]
    public async Task<IActionResult> ReceiveGoods(Guid id, [FromBody] ReceiveGoodsRequest request)
    {
        var command = new ReceiveGoodsCommand(id, CurrentUserId, request.Notes, request.Lines);
        var result = await _mediator.Send(command);
        if (result.IsFailure)
            return ToActionResult(result);
        return Created($"/api/v1/procurement/grn/{result.Value!.GrnId}", result.Value);
    }
}

public record ReceiveGoodsRequest(string? Notes, List<GoodsReceiptLineInput> Lines);
