using GeorgiaERP.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Products.Queries;

public record ExportProductsQuery(string? Search, Guid? CategoryId, bool? IsActive) : IRequest<byte[]>;

public class ExportProductsQueryHandler : IRequestHandler<ExportProductsQuery, byte[]>
{
    private readonly IAppDbContext _dbContext;
    private readonly IExcelService _excelService;

    public ExportProductsQueryHandler(IAppDbContext dbContext, IExcelService excelService)
    {
        _dbContext = dbContext;
        _excelService = excelService;
    }

    public async Task<byte[]> Handle(ExportProductsQuery request, CancellationToken ct)
    {
        var query = _dbContext.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.ToLower();
            query = query.Where(p =>
                p.Sku.ToLower().Contains(search) ||
                p.Name.ToLower().Contains(search) ||
                (p.NameKa != null && p.NameKa.ToLower().Contains(search)));
        }

        if (request.CategoryId.HasValue)
            query = query.Where(p => p.CategoryId == request.CategoryId.Value);

        if (request.IsActive.HasValue)
            query = query.Where(p => p.IsActive == request.IsActive.Value);

        var rows = await query
            .OrderBy(p => p.Name)
            .Select(p => new ProductExportRow(
                p.Sku,
                p.Name,
                p.NameKa,
                p.Category.Name,
                p.UnitOfMeasure,
                p.VatApplicable,
                p.WeightKg,
                p.MinStockLevel,
                p.MaxStockLevel,
                p.IsActive,
                p.CreatedAt))
            .ToListAsync(ct);

        return _excelService.ExportProducts(rows);
    }
}
