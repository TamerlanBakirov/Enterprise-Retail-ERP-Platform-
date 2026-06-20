using GeorgiaERP.Application.Inventory.Commands;
using GeorgiaERP.Application.Inventory.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GeorgiaERP.Api.Controllers;

[Authorize]
public class InventoryController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public InventoryController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("stock-levels")]
    public async Task<IActionResult> GetStockLevels(
        [FromQuery] Guid? warehouseId = null,
        [FromQuery] Guid? productId = null,
        [FromQuery] bool lowStockOnly = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var result = await _mediator.Send(new GetStockLevelsQuery(warehouseId, productId, lowStockOnly, page, pageSize));
        return Ok(result);
    }

    [HttpGet("movements")]
    public async Task<IActionResult> GetStockMovements(
        [FromQuery] Guid? warehouseId = null,
        [FromQuery] Guid? productId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var result = await _mediator.Send(new GetStockMovementsQuery(warehouseId, productId, page, pageSize));
        return Ok(result);
    }

    [HttpPost("adjust")]
    public async Task<IActionResult> AdjustStock([FromBody] AdjustStockCommand command)
    {
        var result = await _mediator.Send(command);
        return result.IsSuccess ? Ok() : BadRequest(new { error = result.Error });
    }

    // Transfer Orders

    [HttpGet("transfers")]
    public async Task<IActionResult> GetTransferOrders(
        [FromQuery] Guid? warehouseId = null,
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetTransferOrdersQuery(warehouseId, status, page, pageSize));
        return Ok(result);
    }

    [HttpPost("transfers")]
    public async Task<IActionResult> CreateTransferOrder([FromBody] CreateTransferOrderCommand command)
    {
        var result = await _mediator.Send(command);
        return result.IsSuccess ? Created($"/api/v1/inventory/transfers/{result.Value!.Id}", result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPost("transfers/{id:guid}/approve")]
    public async Task<IActionResult> ApproveTransfer(Guid id)
    {
        var result = await _mediator.Send(new ApproveTransferCommand(id, CurrentUserId));
        return result.IsSuccess ? Ok() : BadRequest(new { error = result.Error });
    }

    [HttpPost("transfers/{id:guid}/ship")]
    public async Task<IActionResult> ShipTransfer(Guid id)
    {
        var result = await _mediator.Send(new ShipTransferCommand(id));
        return result.IsSuccess ? Ok() : BadRequest(new { error = result.Error });
    }

    [HttpPost("transfers/{id:guid}/receive")]
    public async Task<IActionResult> ReceiveTransfer(Guid id, [FromBody] ReceiveTransferRequest? request = null)
    {
        var result = await _mediator.Send(new ReceiveTransferCommand(id, request?.Lines));
        return result.IsSuccess ? Ok() : BadRequest(new { error = result.Error });
    }

    [HttpPost("transfers/{id:guid}/cancel")]
    public async Task<IActionResult> CancelTransfer(Guid id)
    {
        var result = await _mediator.Send(new CancelTransferCommand(id));
        return result.IsSuccess ? Ok() : BadRequest(new { error = result.Error });
    }

    // Stock Counts

    [HttpGet("counts")]
    public async Task<IActionResult> GetStockCounts(
        [FromQuery] Guid? warehouseId = null,
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetStockCountsQuery(warehouseId, status, page, pageSize));
        return Ok(result);
    }

    [HttpPost("counts")]
    public async Task<IActionResult> CreateStockCount([FromBody] CreateStockCountCommand command)
    {
        var result = await _mediator.Send(command);
        return result.IsSuccess ? Created($"/api/v1/inventory/counts/{result.Value!.Id}", result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPost("counts/{countId:guid}/lines/{lineId:guid}/record")]
    public async Task<IActionResult> RecordCountLine(Guid countId, Guid lineId, [FromBody] RecordCountRequest request)
    {
        var result = await _mediator.Send(new RecordCountLineCommand(countId, lineId, request.CountedQty, CurrentUserId));
        return result.IsSuccess ? Ok() : BadRequest(new { error = result.Error });
    }

    [HttpPost("counts/{countId:guid}/complete")]
    public async Task<IActionResult> CompleteStockCount(Guid countId)
    {
        var result = await _mediator.Send(new CompleteStockCountCommand(countId, CurrentUserId));
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }
}

public record ReceiveTransferRequest(List<ReceiveLineInput>? Lines = null);
public record RecordCountRequest(decimal CountedQty);

