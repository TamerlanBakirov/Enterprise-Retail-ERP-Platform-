using GeorgiaERP.Application.Products.Commands;
using GeorgiaERP.Application.Products.DTOs;
using GeorgiaERP.Application.Products.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GeorgiaERP.Api.Controllers;

/// <summary>
/// Product catalog management including CRUD operations, categories, and barcode tracking.
/// Supports Georgian locale with bilingual product names (EN/KA).
/// </summary>
[Authorize]
[Tags("Products")]
public class ProductsController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public ProductsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Retrieves a paginated list of products with optional filtering.
    /// </summary>
    [HttpGet]
    [EnableRateLimiting("read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProducts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] bool? isActive = null)
    {
        var query = new GetProductsQuery(page, pageSize, search, categoryId, isActive);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Gets a single product by its unique identifier.
    /// </summary>
    [HttpGet("{id:guid}")]
    [EnableRateLimiting("read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProduct(Guid id)
    {
        var result = await _mediator.Send(new GetProductByIdQuery(id));
        if (result is null)
            return NotFound();
        return Ok(result);
    }

    /// <summary>
    /// Creates a new product in the catalog.
    /// </summary>
    [HttpPost]
    [EnableRateLimiting("write")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request)
    {
        var command = new CreateProductCommand(
            request.Sku,
            request.Name,
            request.NameKa,
            request.Description,
            request.CategoryId,
            request.UnitOfMeasure,
            request.VatApplicable,
            request.WeightKg,
            request.VolumeL,
            request.WidthCm,
            request.HeightCm,
            request.DepthCm,
            request.MinStockLevel,
            request.MaxStockLevel,
            request.ReorderPoint,
            request.ReorderQty,
            request.IsSerialized,
            request.IsBatchTracked,
            request.HasExpiry,
            request.Barcodes,
            CurrentUserId);

        var result = await _mediator.Send(command);

        if (result.IsFailure)
            return ToActionResult(result);

        return CreatedAtAction(nameof(GetProduct), new { id = result.Value!.Id }, result.Value);
    }

    /// <summary>
    /// Updates an existing product's details.
    /// </summary>
    [HttpPut("{id:guid}")]
    [EnableRateLimiting("write")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProduct(Guid id, [FromBody] UpdateProductRequest request)
    {
        var command = new UpdateProductCommand(
            id,
            request.Name,
            request.NameKa,
            request.Description,
            request.CategoryId,
            request.UnitOfMeasure,
            request.VatApplicable,
            request.WeightKg,
            request.MinStockLevel,
            request.MaxStockLevel,
            request.ReorderPoint,
            request.ReorderQty,
            request.IsActive);

        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    /// <summary>
    /// Soft-deletes a product by marking it inactive.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [EnableRateLimiting("write")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProduct(Guid id)
    {
        var result = await _mediator.Send(new DeleteProductCommand(id));
        if (result.IsFailure)
            return ToActionResult(result);
        return NoContent();
    }

    [HttpGet("barcode/{barcode}")]
    [EnableRateLimiting("read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProductByBarcode(string barcode)
    {
        var result = await _mediator.Send(new GetProductByBarcodeQuery(barcode));
        if (result is null)
            return NotFound();
        return Ok(result);
    }

    [HttpGet("categories")]
    [EnableRateLimiting("read")]
    public async Task<IActionResult> GetCategories(
        [FromQuery] Guid? parentId = null,
        [FromQuery] bool? isActive = null)
    {
        var result = await _mediator.Send(new GetCategoriesQuery(parentId, isActive));
        return Ok(result);
    }
}
