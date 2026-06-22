using GeorgiaERP.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Export;

// ── Export Request/Response ──────────────────────────────────────────

/// <summary>
/// Represents an export result containing the file bytes and metadata.
/// </summary>
public sealed record ExportResult
{
    public required byte[] FileBytes { get; init; }
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
}

// ── Products Export ─────────────────────────────────────────────────

public sealed record ExportProductsQuery(
    string? Search = null,
    Guid? CategoryId = null,
    bool? IsActive = null) : IRequest<ExportResult>;

public sealed class ExportProductsHandler : IRequestHandler<ExportProductsQuery, ExportResult>
{
    private readonly IAppDbContext _db;
    private readonly IExportService _export;

    public ExportProductsHandler(IAppDbContext db, IExportService export)
    {
        _db = db;
        _export = export;
    }

    public async Task<ExportResult> Handle(ExportProductsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Products.AsNoTracking()
            .Include(p => p.Category)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
            query = query.Where(p => p.Name.Contains(request.Search) || p.Sku.Contains(request.Search));
        if (request.CategoryId.HasValue)
            query = query.Where(p => p.CategoryId == request.CategoryId.Value);
        if (request.IsActive.HasValue)
            query = query.Where(p => p.IsActive == request.IsActive.Value);

        var products = await query.OrderBy(p => p.Sku).ToListAsync(cancellationToken);

        var columns = new List<ExportColumn<Domain.Products.Product>>
        {
            new() { Header = "SKU", Selector = p => p.Sku },
            new() { Header = "Name", Selector = p => p.Name },
            new() { Header = "Name (KA)", Selector = p => p.NameKa },
            new() { Header = "Category", Selector = p => p.Category?.Name },
            new() { Header = "Unit", Selector = p => p.UnitOfMeasure },
            new() { Header = "VAT Applicable", Selector = p => p.VatApplicable },
            new() { Header = "Weight (kg)", Selector = p => p.WeightKg, Format = "N3" },
            new() { Header = "Min Stock", Selector = p => p.MinStockLevel, Format = "N0" },
            new() { Header = "Max Stock", Selector = p => p.MaxStockLevel, Format = "N0" },
            new() { Header = "Reorder Point", Selector = p => p.ReorderPoint, Format = "N0" },
            new() { Header = "Active", Selector = p => p.IsActive },
        };

        var bytes = _export.ToCsv(products, columns);
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss");

        return new ExportResult
        {
            FileBytes = bytes,
            FileName = $"products-{timestamp}.csv",
            ContentType = "text/csv"
        };
    }
}

// ── Inventory Report Export ─────────────────────────────────────────

public sealed record ExportInventoryQuery(
    Guid? WarehouseId = null,
    bool LowStockOnly = false) : IRequest<ExportResult>;

public sealed class ExportInventoryHandler : IRequestHandler<ExportInventoryQuery, ExportResult>
{
    private readonly IAppDbContext _db;
    private readonly IExportService _export;

    public ExportInventoryHandler(IAppDbContext db, IExportService export)
    {
        _db = db;
        _export = export;
    }

    public async Task<ExportResult> Handle(ExportInventoryQuery request, CancellationToken cancellationToken)
    {
        var query = from s in _db.StockLevels.AsNoTracking()
                    join p in _db.Products.AsNoTracking() on s.ProductId equals p.Id
                    join w in _db.Warehouses.AsNoTracking() on s.WarehouseId equals w.Id
                    select new InventoryExportRow
                    {
                        Sku = p.Sku,
                        ProductName = p.Name,
                        WarehouseName = w.Name,
                        QuantityOnHand = s.QuantityOnHand,
                        QuantityReserved = s.QuantityReserved,
                        QuantityAvailable = s.QuantityOnHand - s.QuantityReserved,
                        MinStockLevel = p.MinStockLevel,
                        ReorderPoint = p.ReorderPoint,
                        IsLowStock = p.MinStockLevel.HasValue && s.QuantityOnHand <= p.MinStockLevel.Value
                    };

        if (request.WarehouseId.HasValue)
            query = query.Where(r => _db.StockLevels.Any(s => s.WarehouseId == request.WarehouseId.Value));
        if (request.LowStockOnly)
            query = query.Where(r => r.IsLowStock);

        var rows = await query.OrderBy(r => r.Sku).ThenBy(r => r.WarehouseName).ToListAsync(cancellationToken);

        var columns = new List<ExportColumn<InventoryExportRow>>
        {
            new() { Header = "SKU", Selector = r => r.Sku },
            new() { Header = "Product", Selector = r => r.ProductName },
            new() { Header = "Warehouse", Selector = r => r.WarehouseName },
            new() { Header = "Qty On Hand", Selector = r => r.QuantityOnHand, Format = "N2" },
            new() { Header = "Qty Reserved", Selector = r => r.QuantityReserved, Format = "N2" },
            new() { Header = "Qty Available", Selector = r => r.QuantityAvailable, Format = "N2" },
            new() { Header = "Min Stock", Selector = r => r.MinStockLevel, Format = "N0" },
            new() { Header = "Reorder Point", Selector = r => r.ReorderPoint, Format = "N0" },
            new() { Header = "Low Stock", Selector = r => r.IsLowStock },
        };

        var bytes = _export.ToCsv(rows, columns);
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss");

        return new ExportResult
        {
            FileBytes = bytes,
            FileName = $"inventory-{timestamp}.csv",
            ContentType = "text/csv"
        };
    }
}

internal sealed class InventoryExportRow
{
    public string Sku { get; init; } = default!;
    public string ProductName { get; init; } = default!;
    public string WarehouseName { get; init; } = default!;
    public decimal QuantityOnHand { get; init; }
    public decimal QuantityReserved { get; init; }
    public decimal QuantityAvailable { get; init; }
    public decimal? MinStockLevel { get; init; }
    public decimal? ReorderPoint { get; init; }
    public bool IsLowStock { get; init; }
}

// ── Sales Report Export ─────────────────────────────────────────────

public sealed record ExportSalesQuery(
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    Guid? StoreId = null) : IRequest<ExportResult>;

public sealed class ExportSalesHandler : IRequestHandler<ExportSalesQuery, ExportResult>
{
    private readonly IAppDbContext _db;
    private readonly IExportService _export;

    public ExportSalesHandler(IAppDbContext db, IExportService export)
    {
        _db = db;
        _export = export;
    }

    public async Task<ExportResult> Handle(ExportSalesQuery request, CancellationToken cancellationToken)
    {
        var query = _db.PosTransactions.AsNoTracking().AsQueryable();

        if (request.StoreId.HasValue)
        {
            query = query.Where(t => t.StoreId == request.StoreId.Value);
        }

        // Load transactions, then filter/sort by DateTimeOffset in memory
        // to avoid SQLite/DateTimeOffset translation issues.
        var allTransactions = await query.ToListAsync(cancellationToken);

        var from = request.From ?? DateTimeOffset.UtcNow.AddMonths(-1);
        var to = request.To ?? DateTimeOffset.UtcNow;

        var transactions = allTransactions
            .Where(t => t.CreatedAt >= from && t.CreatedAt <= to)
            .OrderByDescending(t => t.CreatedAt)
            .ToList();

        var columns = new List<ExportColumn<Domain.POS.PosTransaction>>
        {
            new() { Header = "Transaction #", Selector = t => t.TransactionNumber },
            new() { Header = "Date", Selector = t => t.CreatedAt },
            new() { Header = "Type", Selector = t => t.TransactionType },
            new() { Header = "Subtotal", Selector = t => t.Subtotal, Format = "N2" },
            new() { Header = "VAT", Selector = t => t.VatTotal, Format = "N2" },
            new() { Header = "Discount", Selector = t => t.DiscountTotal, Format = "N2" },
            new() { Header = "Total", Selector = t => t.Total, Format = "N2" },
            new() { Header = "Status", Selector = t => t.Status },
        };

        var bytes = _export.ToCsv(transactions, columns);
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss");

        return new ExportResult
        {
            FileBytes = bytes,
            FileName = $"sales-{from:yyyyMMdd}-to-{to:yyyyMMdd}-{timestamp}.csv",
            ContentType = "text/csv"
        };
    }
}

// ── Customers Export ────────────────────────────────────────────────

public sealed record ExportCustomersQuery(
    string? Search = null,
    bool? IsActive = null) : IRequest<ExportResult>;

public sealed class ExportCustomersHandler : IRequestHandler<ExportCustomersQuery, ExportResult>
{
    private readonly IAppDbContext _db;
    private readonly IExportService _export;

    public ExportCustomersHandler(IAppDbContext db, IExportService export)
    {
        _db = db;
        _export = export;
    }

    public async Task<ExportResult> Handle(ExportCustomersQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Customers.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
            query = query.Where(c => c.FirstName.Contains(request.Search)
                || c.LastName.Contains(request.Search)
                || (c.Email != null && c.Email.Contains(request.Search))
                || c.CustomerNumber.Contains(request.Search));
        if (request.IsActive.HasValue)
            query = query.Where(c => c.IsActive == request.IsActive.Value);

        var customers = await query.OrderBy(c => c.CustomerNumber).ToListAsync(cancellationToken);

        var columns = new List<ExportColumn<Domain.CRM.Customer>>
        {
            new() { Header = "Customer #", Selector = c => c.CustomerNumber },
            new() { Header = "First Name", Selector = c => c.FirstName },
            new() { Header = "Last Name", Selector = c => c.LastName },
            new() { Header = "First Name (KA)", Selector = c => c.FirstNameKa },
            new() { Header = "Last Name (KA)", Selector = c => c.LastNameKa },
            new() { Header = "Company", Selector = c => c.CompanyName },
            new() { Header = "TIN", Selector = c => c.Tin },
            new() { Header = "Phone", Selector = c => c.Phone },
            new() { Header = "Email", Selector = c => c.Email },
            new() { Header = "Loyalty Tier", Selector = c => c.LoyaltyTier },
            new() { Header = "Loyalty Points", Selector = c => c.LoyaltyPoints },
            new() { Header = "Total Purchases", Selector = c => c.TotalPurchases, Format = "N2" },
            new() { Header = "Total Visits", Selector = c => c.TotalVisits },
            new() { Header = "Active", Selector = c => c.IsActive },
        };

        var bytes = _export.ToCsv(customers, columns);
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss");

        return new ExportResult
        {
            FileBytes = bytes,
            FileName = $"customers-{timestamp}.csv",
            ContentType = "text/csv"
        };
    }
}

// ── Audit Log Export ────────────────────────────────────────────────

public sealed record ExportAuditLogQuery(
    string? EntityType = null,
    Guid? UserId = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null) : IRequest<ExportResult>;

public sealed class ExportAuditLogHandler : IRequestHandler<ExportAuditLogQuery, ExportResult>
{
    private readonly IAppDbContext _db;
    private readonly IExportService _export;

    public ExportAuditLogHandler(IAppDbContext db, IExportService export)
    {
        _db = db;
        _export = export;
    }

    public async Task<ExportResult> Handle(ExportAuditLogQuery request, CancellationToken cancellationToken)
    {
        var query = _db.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.EntityType))
            query = query.Where(a => a.EntityType == request.EntityType);
        if (request.UserId.HasValue)
            query = query.Where(a => a.UserId == request.UserId.Value);

        // Load all matching records, then filter by date in memory
        // to avoid SQLite/DateTimeOffset translation issues.
        var allLogs = await query.ToListAsync(cancellationToken);

        IEnumerable<Domain.Common.AuditLog> filtered = allLogs;
        if (request.From.HasValue)
            filtered = filtered.Where(a => a.Timestamp >= request.From.Value);
        if (request.To.HasValue)
            filtered = filtered.Where(a => a.Timestamp <= request.To.Value);

        var logs = filtered
            .OrderByDescending(a => a.Timestamp)
            .Take(10000) // Safety limit for export
            .ToList();

        var columns = new List<ExportColumn<Domain.Common.AuditLog>>
        {
            new() { Header = "Timestamp", Selector = a => a.Timestamp },
            new() { Header = "Entity Type", Selector = a => a.EntityType },
            new() { Header = "Entity ID", Selector = a => a.EntityId },
            new() { Header = "Action", Selector = a => a.Action },
            new() { Header = "User ID", Selector = a => a.UserId },
            new() { Header = "IP Address", Selector = a => a.IpAddress },
            new() { Header = "Changed Properties", Selector = a => a.ChangedProperties },
        };

        var bytes = _export.ToCsv(logs, columns);
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss");

        return new ExportResult
        {
            FileBytes = bytes,
            FileName = $"audit-log-{timestamp}.csv",
            ContentType = "text/csv"
        };
    }
}
