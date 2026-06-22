using GeorgiaERP.Application.Common;
using GeorgiaERP.Application.Products.Commands;
using GeorgiaERP.Application.Products.DTOs;
using GeorgiaERP.Application.Products.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GeorgiaERP.Api.Controllers;

[Authorize]
public class ProductsController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public ProductsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
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

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetProduct(Guid id)
    {
        var result = await _mediator.Send(new GetProductByIdQuery(id));
        if (result is null)
            return NotFound();
        return Ok(result);
    }

    [HttpPost]
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

    [HttpGet("export")]
    public async Task<IActionResult> ExportProducts(
        [FromQuery] string? search,
        [FromQuery] Guid? categoryId,
        [FromQuery] bool? isActive)
    {
        var bytes = await _mediator.Send(new ExportProductsQuery(search, categoryId, isActive));
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "products.xlsx");
    }

    [HttpGet("import-template")]
    public IActionResult GetImportTemplate([FromServices] IExcelService excelService)
    {
        var bytes = excelService.GenerateProductImportTemplate();
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "product-import-template.xlsx");
    }

    [HttpPost("import")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> ImportProducts(IFormFile file)
    {
        if (file.Length == 0)
            return BadRequest(new { error = "File is empty" });

        using var stream = file.OpenReadStream();
        var result = await _mediator.Send(new ImportProductsCommand(stream, CurrentUserId));
        return ToActionResult(result);
    }

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories(
        [FromQuery] Guid? parentId = null,
        [FromQuery] bool? isActive = null)
    {
        var result = await _mediator.Send(new GetCategoriesQuery(parentId, isActive));
        return Ok(result);
    }
}
