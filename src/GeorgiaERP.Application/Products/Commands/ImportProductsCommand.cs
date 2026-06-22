using GeorgiaERP.Application.Common;
using GeorgiaERP.Domain.Products;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Products.Commands;

public record ImportProductsCommand(Stream FileStream, Guid CreatedBy) : IRequest<Result<ImportProductsResult>>;

public record ImportProductsResult(int Created, int Updated, int Skipped, List<string> Errors);

public class ImportProductsCommandHandler : IRequestHandler<ImportProductsCommand, Result<ImportProductsResult>>
{
    private readonly IAppDbContext _dbContext;
    private readonly IExcelService _excelService;

    public ImportProductsCommandHandler(IAppDbContext dbContext, IExcelService excelService)
    {
        _dbContext = dbContext;
        _excelService = excelService;
    }

    public async Task<Result<ImportProductsResult>> Handle(ImportProductsCommand request, CancellationToken ct)
    {
        var parseResult = _excelService.ParseProductImport(request.FileStream);
        if (parseResult.IsFailure)
            return Result.ValidationFailure<ImportProductsResult>(parseResult.Errors);

        var rows = parseResult.Value!;
        if (rows.Count == 0)
            return Result.Success(new ImportProductsResult(0, 0, 0, new List<string>()));

        var categoryCodes = rows.Select(r => r.CategoryCode).Distinct().ToList();
        var categories = await _dbContext.Categories
            .Where(c => categoryCodes.Contains(c.Code))
            .ToDictionaryAsync(c => c.Code, c => c.Id, ct);

        var skus = rows.Select(r => r.Sku).Distinct().ToList();
        var existingProducts = await _dbContext.Products
            .Where(p => skus.Contains(p.Sku))
            .ToDictionaryAsync(p => p.Sku, p => p, ct);

        int created = 0, updated = 0, skipped = 0;
        var errors = new List<string>();

        foreach (var row in rows)
        {
            if (!categories.TryGetValue(row.CategoryCode, out var categoryId))
            {
                errors.Add($"Row {row.RowNumber}: Category '{row.CategoryCode}' not found");
                skipped++;
                continue;
            }

            if (existingProducts.TryGetValue(row.Sku, out var existing))
            {
                existing.Update(
                    row.Name,
                    row.NameKa,
                    row.UnitOfMeasure,
                    row.VatApplicable,
                    row.WeightKg,
                    row.MinStockLevel,
                    row.MaxStockLevel);
                updated++;
            }
            else
            {
                var product = Product.Create(
                    sku: row.Sku,
                    name: row.Name,
                    categoryId: categoryId,
                    unitOfMeasure: row.UnitOfMeasure,
                    vatApplicable: row.VatApplicable,
                    nameKa: row.NameKa,
                    weightKg: row.WeightKg,
                    minStockLevel: row.MinStockLevel,
                    maxStockLevel: row.MaxStockLevel);

                _dbContext.Products.Add(product);
                created++;
            }
        }

        await _dbContext.SaveChangesAsync(ct);

        return Result.Success(new ImportProductsResult(created, updated, skipped, errors));
    }
}
