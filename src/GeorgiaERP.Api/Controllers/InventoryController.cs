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
}
