using GeorgiaERP.Application.Webhooks.Commands;
using GeorgiaERP.Application.Webhooks.DTOs;
using GeorgiaERP.Application.Webhooks.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GeorgiaERP.Api.Controllers;

/// <summary>
/// Webhook subscription management for receiving event notifications.
/// External systems can subscribe to events (order created, waybill status changed,
/// low stock) and receive HTTP POST callbacks with HMAC-SHA256 signed payloads.
/// </summary>
[Authorize(Roles = "super_admin,admin")]
[Tags("Webhooks")]
[EnableRateLimiting("read")]
public class WebhooksController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public WebhooksController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Lists all webhook subscriptions. Optionally filter by active status.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWebhooks([FromQuery] bool? isActive = null)
    {
        var result = await _mediator.Send(new GetWebhooksQuery(isActive));
        return Ok(result);
    }

    /// <summary>
    /// Gets a specific webhook subscription by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWebhook(Guid id)
    {
        var result = await _mediator.Send(new GetWebhookByIdQuery(id));
        if (result is null)
            return NotFound();
        return Ok(result);
    }

    /// <summary>
    /// Creates a new webhook subscription. The secret is used for HMAC-SHA256
    /// signature verification of delivered payloads.
    /// </summary>
    /// <remarks>
    /// Available event types: order.created, waybill.status_changed, stock.low,
    /// product.created, product.updated, user.created, purchase_order.approved,
    /// invoice.created, or "*" for all events.
    /// </remarks>
    [HttpPost]
    [EnableRateLimiting("write")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateWebhook([FromBody] CreateWebhookRequest request)
    {
        var command = new CreateWebhookCommand(
            request.Name, request.Url, request.Secret, request.EventTypes, request.MaxRetries);

        var result = await _mediator.Send(command);
        if (result.IsFailure)
            return ToActionResult(result);

        return CreatedAtAction(nameof(GetWebhook), new { id = result.Value!.Id }, result.Value);
    }

    /// <summary>
    /// Updates an existing webhook subscription.
    /// </summary>
    [HttpPut("{id:guid}")]
    [EnableRateLimiting("write")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateWebhook(Guid id, [FromBody] UpdateWebhookRequest request)
    {
        var command = new UpdateWebhookCommand(id, request.Name, request.Url, request.EventTypes, request.MaxRetries);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    /// <summary>
    /// Deletes a webhook subscription and all its delivery logs.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [EnableRateLimiting("write")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteWebhook(Guid id)
    {
        var result = await _mediator.Send(new DeleteWebhookCommand(id));
        if (result.IsFailure)
            return ToActionResult(result);
        return NoContent();
    }

    /// <summary>
    /// Activates a disabled webhook subscription.
    /// </summary>
    [HttpPost("{id:guid}/activate")]
    [EnableRateLimiting("write")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActivateWebhook(Guid id)
    {
        var result = await _mediator.Send(new ActivateWebhookCommand(id));
        return ToActionResult(result);
    }

    /// <summary>
    /// Deactivates a webhook subscription (stops delivery).
    /// </summary>
    [HttpPost("{id:guid}/deactivate")]
    [EnableRateLimiting("write")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateWebhook(Guid id)
    {
        var result = await _mediator.Send(new DeactivateWebhookCommand(id));
        return ToActionResult(result);
    }

    /// <summary>
    /// Gets delivery history for a webhook subscription.
    /// </summary>
    [HttpGet("{id:guid}/deliveries")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDeliveryLogs(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetWebhookDeliveryLogsQuery(id, page, pageSize));
        return Ok(result);
    }
}
