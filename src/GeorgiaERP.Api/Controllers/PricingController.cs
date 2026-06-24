using GeorgiaERP.Application.Pricing.Commands;
using GeorgiaERP.Application.Pricing.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GeorgiaERP.Api.Controllers;

/// <summary>
/// Pricing management including price lists, promotions, and discount rules.
/// </summary>
[Authorize]
[Tags("Pricing")]
[EnableRateLimiting("read")]
public class PricingController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public PricingController(IMediator mediator) => _mediator = mediator;

    [HttpGet("price-lists")]
    public async Task<IActionResult> GetPriceLists(
        [FromQuery] string? priceType = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        return Ok(await _mediator.Send(new GetPriceListsQuery(priceType, isActive, page, pageSize)));
    }

    [HttpPost("price-lists")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> CreatePriceList([FromBody] CreatePriceListCommand command)
    {
        var result = await _mediator.Send(command);
        if (result.IsFailure)
            return ToActionResult(result);
        return Created($"/api/v1/pricing/price-lists/{result.Value!.Id}", result.Value);
    }

    [HttpGet("price-lists/{priceListId:guid}/items")]
    public async Task<IActionResult> GetPriceListItems(
        Guid priceListId,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        return Ok(await _mediator.Send(new GetPriceListItemsQuery(priceListId, search, page, pageSize)));
    }

    [HttpPost("prices")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> SetPrice([FromBody] SetPriceCommand command)
    {
        var result = await _mediator.Send(command);
        if (result.IsFailure)
            return ToActionResult(result);
        return Ok(result.Value);
    }

    [HttpGet("promotions")]
    public async Task<IActionResult> GetPromotions(
        [FromQuery] bool? isActive = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        return Ok(await _mediator.Send(new GetPromotionsQuery(isActive, page, pageSize)));
    }

    [HttpPost("promotions")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> CreatePromotion([FromBody] CreatePromotionCommand command)
    {
        var result = await _mediator.Send(command);
        if (result.IsFailure)
            return ToActionResult(result);
        return Created($"/api/v1/pricing/promotions/{result.Value!.Id}", result.Value);
    }

    [HttpPost("promotions/{id:guid}/deactivate")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> DeactivatePromotion(Guid id)
    {
        var result = await _mediator.Send(new SetPromotionStatusCommand(id, IsActive: false));
        return ToActionResult(result);
    }

    [HttpPost("promotions/{id:guid}/activate")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> ActivatePromotion(Guid id)
    {
        var result = await _mediator.Send(new SetPromotionStatusCommand(id, IsActive: true));
        return ToActionResult(result);
    }
}
