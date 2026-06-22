using GeorgiaERP.Application.Warehouse.Commands;
using GeorgiaERP.Application.Warehouse.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GeorgiaERP.Api.Controllers;

/// <summary>
/// Warehouse operations including location management, receiving orders,
/// and shipping order workflows.
/// </summary>
[Authorize]
[Tags("Warehouse")]
[EnableRateLimiting("read")]
public class WarehouseController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public WarehouseController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // Warehouse CRUD

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetWarehouse(Guid id)
    {
        var result = await _mediator.Send(new GetWarehouseByIdQuery(id));
        return ToActionResult(result);
    }

    [HttpPost]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> CreateWarehouse([FromBody] CreateWarehouseCommand command)
    {
        var result = await _mediator.Send(command);
        if (result.IsFailure)
            return ToActionResult(result);
        return Created($"/api/v1/warehouse/{result.Value}", new { id = result.Value });
    }

    [HttpPut("{id:guid}")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> UpdateWarehouse(Guid id, [FromBody] UpdateWarehouseRequest request)
    {
        var command = new UpdateWarehouseCommand(id, request.Name, request.NameKa,
            request.Address, request.City, request.Region, request.LinkedStoreId);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPost("{id:guid}/activate")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> ActivateWarehouse(Guid id)
    {
        var result = await _mediator.Send(new ActivateWarehouseCommand(id));
        return ToActionResult(result);
    }

    [HttpPost("{id:guid}/deactivate")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> DeactivateWarehouse(Guid id)
    {
        var result = await _mediator.Send(new DeactivateWarehouseCommand(id));
        return ToActionResult(result);
    }

    // Locations

    [HttpGet("{warehouseId:guid}/locations")]
    public async Task<IActionResult> GetLocations(
        Guid warehouseId,
        [FromQuery] string? locationType = null,
        [FromQuery] bool? isActive = null)
    {
        var result = await _mediator.Send(new GetWarehouseLocationsQuery(warehouseId, locationType, isActive));
        return Ok(result);
    }

    [HttpPost("{warehouseId:guid}/locations")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> CreateLocation(Guid warehouseId, [FromBody] CreateLocationRequest request)
    {
        var command = new CreateWarehouseLocationCommand(
            warehouseId, request.Code, request.Name, request.NameKa,
            request.LocationType, request.ParentLocationId,
            request.SortOrder, request.MaxCapacity, request.Notes);
        var result = await _mediator.Send(command);
        if (result.IsFailure)
            return ToActionResult(result);
        return Created($"/api/v1/warehouse/{warehouseId}/locations/{result.Value}", new { id = result.Value });
    }

    [HttpPut("locations/{id:guid}")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> UpdateLocation(Guid id, [FromBody] UpdateLocationRequest request)
    {
        var command = new UpdateWarehouseLocationCommand(
            id, request.Name, request.NameKa, request.SortOrder, request.MaxCapacity, request.Notes);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPost("locations/{id:guid}/deactivate")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> DeactivateLocation(Guid id)
    {
        var result = await _mediator.Send(new DeactivateWarehouseLocationCommand(id));
        return ToActionResult(result);
    }

    // Receiving Orders

    [HttpGet("receiving")]
    public async Task<IActionResult> GetReceivingOrders(
        [FromQuery] Guid? warehouseId = null,
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetReceivingOrdersQuery(warehouseId, status, page, pageSize));
        return Ok(result);
    }

    [HttpGet("receiving/{id:guid}")]
    public async Task<IActionResult> GetReceivingOrder(Guid id)
    {
        var result = await _mediator.Send(new GetReceivingOrderByIdQuery(id));
        return ToActionResult(result);
    }

    [HttpPost("receiving")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> CreateReceivingOrder([FromBody] CreateReceivingOrderCommand command)
    {
        var result = await _mediator.Send(command);
        if (result.IsFailure)
            return ToActionResult(result);
        return Created($"/api/v1/warehouse/receiving/{result.Value!.Id}", result.Value);
    }

    [HttpPost("receiving/{id:guid}/start")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> StartReceiving(Guid id)
    {
        var result = await _mediator.Send(new StartReceivingCommand(id));
        return ToActionResult(result);
    }

    [HttpPost("receiving/{orderId:guid}/lines/{lineId:guid}/receive")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> ReceiveLine(
        Guid orderId, Guid lineId, [FromBody] ReceiveLineRequest request)
    {
        var command = new ReceiveLineCommand(
            orderId, lineId, request.ReceivedQty, request.DamagedQty,
            request.BatchNumber, request.SerialNumber, request.LocationId);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPost("receiving/{id:guid}/complete")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> CompleteReceiving(Guid id)
    {
        var result = await _mediator.Send(new CompleteReceivingCommand(id, CurrentUserId));
        return ToActionResult(result);
    }

    [HttpPost("receiving/{id:guid}/cancel")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> CancelReceiving(Guid id)
    {
        var result = await _mediator.Send(new CancelReceivingCommand(id));
        return ToActionResult(result);
    }

    // Shipping Orders

    [HttpGet("shipping")]
    public async Task<IActionResult> GetShippingOrders(
        [FromQuery] Guid? warehouseId = null,
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetShippingOrdersQuery(warehouseId, status, page, pageSize));
        return Ok(result);
    }

    [HttpGet("shipping/{id:guid}")]
    public async Task<IActionResult> GetShippingOrder(Guid id)
    {
        var result = await _mediator.Send(new GetShippingOrderByIdQuery(id));
        return ToActionResult(result);
    }

    [HttpPost("shipping")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> CreateShippingOrder([FromBody] CreateShippingOrderCommand command)
    {
        var result = await _mediator.Send(command);
        if (result.IsFailure)
            return ToActionResult(result);
        return Created($"/api/v1/warehouse/shipping/{result.Value!.Id}", result.Value);
    }

    [HttpPost("shipping/{id:guid}/pick")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> StartPicking(Guid id)
    {
        var result = await _mediator.Send(new StartPickingCommand(id));
        return ToActionResult(result);
    }

    [HttpPost("shipping/{orderId:guid}/lines/{lineId:guid}/pick")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> PickLine(
        Guid orderId, Guid lineId, [FromBody] PickLineRequest request)
    {
        var command = new PickLineCommand(orderId, lineId, request.PickedQty, request.LocationId);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPost("shipping/{id:guid}/pack")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> PackOrder(Guid id)
    {
        var result = await _mediator.Send(new PackShippingOrderCommand(id));
        return ToActionResult(result);
    }

    [HttpPost("shipping/{id:guid}/ship")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> ShipOrder(Guid id, [FromBody] ShipOrderRequest request)
    {
        var command = new ShipOrderCommand(id, CurrentUserId, request.TrackingNumber);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPost("shipping/{id:guid}/cancel")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> CancelShipping(Guid id)
    {
        var result = await _mediator.Send(new CancelShippingOrderCommand(id));
        return ToActionResult(result);
    }
}

// Request DTOs
public record UpdateWarehouseRequest(
    string Name,
    string? NameKa,
    string? Address,
    string? City,
    string? Region,
    Guid? LinkedStoreId);

public record CreateLocationRequest(
    string Code,
    string Name,
    string? NameKa,
    string LocationType,
    Guid? ParentLocationId,
    int SortOrder = 0,
    int? MaxCapacity = null,
    string? Notes = null);

public record UpdateLocationRequest(
    string Name,
    string? NameKa,
    int SortOrder = 0,
    int? MaxCapacity = null,
    string? Notes = null);

public record ReceiveLineRequest(
    decimal ReceivedQty,
    decimal? DamagedQty = null,
    string? BatchNumber = null,
    string? SerialNumber = null,
    Guid? LocationId = null);

public record PickLineRequest(
    decimal PickedQty,
    Guid? LocationId = null);

public record ShipOrderRequest(
    string? TrackingNumber = null);
