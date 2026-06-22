using System.Globalization;
using GeorgiaERP.Application.Common;
using GeorgiaERP.Domain.Inventory;
using GeorgiaERP.Domain.Products;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Import;

// ── Import Products ───────────────────────────────────────────────────

/// <summary>
/// Imports products from a CSV or Excel file.
/// Required columns: SKU, Name, CategoryId, UnitOfMeasure.
/// Optional columns: NameKa, Description, VatApplicable, WeightKg, MinStockLevel,
///                    MaxStockLevel, ReorderPoint, ReorderQty.
/// </summary>
public sealed record ImportProductsCommand(
    Stream FileStream,
    string ContentType) : IRequest<ImportResult>;

public sealed class ImportProductsHandler : IRequestHandler<ImportProductsCommand, ImportResult>
{
    private readonly IAppDbContext _db;
    private readonly IImportService _importService;

    public ImportProductsHandler(IAppDbContext db, IImportService importService)
    {
        _db = db;
        _importService = importService;
    }

    public async Task<ImportResult> Handle(ImportProductsCommand request, CancellationToken cancellationToken)
    {
        var rows = _importService.ParseRows(request.FileStream, request.ContentType);
        if (rows.Count == 0)
        {
            return new ImportResult
            {
                TotalRows = 0,
                SuccessCount = 0,
                ErrorCount = 0
            };
        }

        var errors = new List<ImportRowError>();
        var productsToAdd = new List<Product>();

        // Preload existing SKUs for duplicate detection
        var existingSkus = await _db.Products
            .AsNoTracking()
            .Select(p => p.Sku)
            .ToListAsync(cancellationToken);
        var skuSet = new HashSet<string>(existingSkus, StringComparer.OrdinalIgnoreCase);

        // Preload valid category IDs
        var validCategoryIds = await _db.Categories
            .AsNoTracking()
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);
        var categoryIdSet = new HashSet<Guid>(validCategoryIds);

        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var rowNum = i + 2; // +2 because row 1 is the header, rows are 0-indexed
            var rowErrors = new List<ImportRowError>();

            // Required: SKU
            var sku = row.GetValueOrDefault("SKU", "").Trim();
            if (string.IsNullOrWhiteSpace(sku))
                rowErrors.Add(new ImportRowError { RowNumber = rowNum, Field = "SKU", Error = "SKU is required" });
            else if (skuSet.Contains(sku))
                rowErrors.Add(new ImportRowError { RowNumber = rowNum, Field = "SKU", Error = "SKU already exists", Value = sku });

            // Required: Name
            var name = row.GetValueOrDefault("Name", "").Trim();
            if (string.IsNullOrWhiteSpace(name))
                rowErrors.Add(new ImportRowError { RowNumber = rowNum, Field = "Name", Error = "Name is required" });

            // Required: CategoryId
            Guid categoryId = Guid.Empty;
            var categoryIdStr = row.GetValueOrDefault("CategoryId", "").Trim();
            if (string.IsNullOrWhiteSpace(categoryIdStr))
                rowErrors.Add(new ImportRowError { RowNumber = rowNum, Field = "CategoryId", Error = "CategoryId is required" });
            else if (!Guid.TryParse(categoryIdStr, out categoryId))
                rowErrors.Add(new ImportRowError { RowNumber = rowNum, Field = "CategoryId", Error = "Invalid GUID format", Value = categoryIdStr });
            else if (!categoryIdSet.Contains(categoryId))
                rowErrors.Add(new ImportRowError { RowNumber = rowNum, Field = "CategoryId", Error = "Category not found", Value = categoryIdStr });

            // Required: UnitOfMeasure
            var unitOfMeasure = row.GetValueOrDefault("UnitOfMeasure", "").Trim();
            if (string.IsNullOrWhiteSpace(unitOfMeasure))
                rowErrors.Add(new ImportRowError { RowNumber = rowNum, Field = "UnitOfMeasure", Error = "UnitOfMeasure is required" });

            // Optional fields
            var nameKa = row.GetValueOrDefault("NameKa", "").Trim();
            var description = row.GetValueOrDefault("Description", "").Trim();

            var vatApplicable = true;
            var vatStr = row.GetValueOrDefault("VatApplicable", "").Trim();
            if (!string.IsNullOrWhiteSpace(vatStr))
            {
                if (!TryParseBool(vatStr, out vatApplicable))
                    rowErrors.Add(new ImportRowError { RowNumber = rowNum, Field = "VatApplicable", Error = "Must be true/false/yes/no", Value = vatStr });
            }

            var weightKg = TryParseOptionalDecimal(row, "WeightKg", rowNum, rowErrors);
            var minStockLevel = TryParseOptionalDecimal(row, "MinStockLevel", rowNum, rowErrors);
            var maxStockLevel = TryParseOptionalDecimal(row, "MaxStockLevel", rowNum, rowErrors);
            var reorderPoint = TryParseOptionalDecimal(row, "ReorderPoint", rowNum, rowErrors);
            var reorderQty = TryParseOptionalDecimal(row, "ReorderQty", rowNum, rowErrors);

            if (rowErrors.Count > 0)
            {
                errors.AddRange(rowErrors);
                continue;
            }

            var product = Product.Create(
                sku, name, categoryId, unitOfMeasure,
                vatApplicable: vatApplicable,
                nameKa: string.IsNullOrWhiteSpace(nameKa) ? null : nameKa,
                description: string.IsNullOrWhiteSpace(description) ? null : description,
                weightKg: weightKg,
                minStockLevel: minStockLevel,
                maxStockLevel: maxStockLevel,
                reorderPoint: reorderPoint,
                reorderQty: reorderQty);

            productsToAdd.Add(product);
            skuSet.Add(sku); // Track for intra-batch duplicate detection
        }

        if (productsToAdd.Count > 0)
        {
            _db.Products.AddRange(productsToAdd);
            await _db.SaveChangesAsync(cancellationToken);
        }

        return new ImportResult
        {
            TotalRows = rows.Count,
            SuccessCount = productsToAdd.Count,
            ErrorCount = errors.Count > 0 ? rows.Count - productsToAdd.Count : 0,
            Errors = errors
        };
    }

    private static decimal? TryParseOptionalDecimal(
        Dictionary<string, string> row, string field, int rowNum, List<ImportRowError> errors)
    {
        var value = row.GetValueOrDefault(field, "").Trim();
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (decimal.TryParse(value, CultureInfo.InvariantCulture, out var result))
            return result;

        errors.Add(new ImportRowError { RowNumber = rowNum, Field = field, Error = "Invalid number format", Value = value });
        return null;
    }

    private static bool TryParseBool(string value, out bool result)
    {
        if (bool.TryParse(value, out result))
            return true;

        if (value.Equals("yes", StringComparison.OrdinalIgnoreCase) || value == "1")
        {
            result = true;
            return true;
        }

        if (value.Equals("no", StringComparison.OrdinalIgnoreCase) || value == "0")
        {
            result = false;
            return true;
        }

        result = false;
        return false;
    }
}

// ── Import Inventory ──────────────────────────────────────────────────

/// <summary>
/// Imports inventory adjustments from a CSV or Excel file.
/// Required columns: SKU, WarehouseId, Quantity.
/// Optional columns: CostPrice.
/// Creates StockLevel records if they don't exist, otherwise adjusts existing stock.
/// </summary>
public sealed record ImportInventoryCommand(
    Stream FileStream,
    string ContentType) : IRequest<ImportResult>;

public sealed class ImportInventoryHandler : IRequestHandler<ImportInventoryCommand, ImportResult>
{
    private readonly IAppDbContext _db;
    private readonly IImportService _importService;

    public ImportInventoryHandler(IAppDbContext db, IImportService importService)
    {
        _db = db;
        _importService = importService;
    }

    public async Task<ImportResult> Handle(ImportInventoryCommand request, CancellationToken cancellationToken)
    {
        var rows = _importService.ParseRows(request.FileStream, request.ContentType);
        if (rows.Count == 0)
        {
            return new ImportResult
            {
                TotalRows = 0,
                SuccessCount = 0,
                ErrorCount = 0
            };
        }

        var errors = new List<ImportRowError>();
        var successCount = 0;

        // Preload product SKU-to-ID mapping
        var productMap = await _db.Products
            .AsNoTracking()
            .Select(p => new { p.Id, p.Sku })
            .ToListAsync(cancellationToken);
        var skuToId = productMap.ToDictionary(p => p.Sku, p => p.Id, StringComparer.OrdinalIgnoreCase);

        // Preload valid warehouse IDs
        var validWarehouseIds = await _db.Warehouses
            .AsNoTracking()
            .Select(w => w.Id)
            .ToListAsync(cancellationToken);
        var warehouseIdSet = new HashSet<Guid>(validWarehouseIds);

        // Preload existing stock levels
        var existingStockLevels = await _db.StockLevels
            .ToListAsync(cancellationToken);
        var stockLevelMap = existingStockLevels
            .ToDictionary(s => (s.ProductId, s.WarehouseId));

        var newStockLevels = new List<StockLevel>();

        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var rowNum = i + 2;
            var rowErrors = new List<ImportRowError>();

            // Required: SKU
            var sku = row.GetValueOrDefault("SKU", "").Trim();
            Guid productId = Guid.Empty;
            if (string.IsNullOrWhiteSpace(sku))
                rowErrors.Add(new ImportRowError { RowNumber = rowNum, Field = "SKU", Error = "SKU is required" });
            else if (!skuToId.TryGetValue(sku, out productId))
                rowErrors.Add(new ImportRowError { RowNumber = rowNum, Field = "SKU", Error = "Product not found", Value = sku });

            // Required: WarehouseId
            Guid warehouseId = Guid.Empty;
            var warehouseIdStr = row.GetValueOrDefault("WarehouseId", "").Trim();
            if (string.IsNullOrWhiteSpace(warehouseIdStr))
                rowErrors.Add(new ImportRowError { RowNumber = rowNum, Field = "WarehouseId", Error = "WarehouseId is required" });
            else if (!Guid.TryParse(warehouseIdStr, out warehouseId))
                rowErrors.Add(new ImportRowError { RowNumber = rowNum, Field = "WarehouseId", Error = "Invalid GUID format", Value = warehouseIdStr });
            else if (!warehouseIdSet.Contains(warehouseId))
                rowErrors.Add(new ImportRowError { RowNumber = rowNum, Field = "WarehouseId", Error = "Warehouse not found", Value = warehouseIdStr });

            // Required: Quantity
            decimal quantity = 0;
            var quantityStr = row.GetValueOrDefault("Quantity", "").Trim();
            if (string.IsNullOrWhiteSpace(quantityStr))
                rowErrors.Add(new ImportRowError { RowNumber = rowNum, Field = "Quantity", Error = "Quantity is required" });
            else if (!decimal.TryParse(quantityStr, CultureInfo.InvariantCulture, out quantity))
                rowErrors.Add(new ImportRowError { RowNumber = rowNum, Field = "Quantity", Error = "Invalid number format", Value = quantityStr });
            else if (quantity <= 0)
                rowErrors.Add(new ImportRowError { RowNumber = rowNum, Field = "Quantity", Error = "Quantity must be positive", Value = quantityStr });

            // Optional: CostPrice
            decimal costPrice = 0;
            var costPriceStr = row.GetValueOrDefault("CostPrice", "").Trim();
            if (!string.IsNullOrWhiteSpace(costPriceStr))
            {
                if (!decimal.TryParse(costPriceStr, CultureInfo.InvariantCulture, out costPrice))
                    rowErrors.Add(new ImportRowError { RowNumber = rowNum, Field = "CostPrice", Error = "Invalid number format", Value = costPriceStr });
            }

            if (rowErrors.Count > 0)
            {
                errors.AddRange(rowErrors);
                continue;
            }

            // Create or adjust stock level
            var key = (productId, warehouseId);
            if (stockLevelMap.TryGetValue(key, out var existingStock))
            {
                existingStock.AddStock(quantity, MovementType.Adjustment);
            }
            else
            {
                var newStock = StockLevel.Create(productId, warehouseId, costPrice);
                newStock.AddStock(quantity, MovementType.Adjustment);
                newStockLevels.Add(newStock);
                stockLevelMap[key] = newStock;
            }

            successCount++;
        }

        if (newStockLevels.Count > 0)
        {
            _db.StockLevels.AddRange(newStockLevels);
        }

        if (successCount > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        return new ImportResult
        {
            TotalRows = rows.Count,
            SuccessCount = successCount,
            ErrorCount = rows.Count - successCount,
            Errors = errors
        };
    }
}
