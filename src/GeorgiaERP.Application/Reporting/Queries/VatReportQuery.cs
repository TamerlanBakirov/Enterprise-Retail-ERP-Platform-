using GeorgiaERP.Application.Common;
using GeorgiaERP.Domain.Compliance;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Reporting.Queries;

public record VatReportQuery(int Year, int Month) : IRequest<VatReport>;

public record VatReport(
    string Period,
    decimal OutputVat,
    decimal InputVat,
    decimal NetVat,
    int FiscalDocumentsTotal,
    int FiscalDocumentsSubmitted,
    int FiscalDocumentsPending,
    int FiscalDocumentsFailed,
    List<VatByDocumentType> ByDocumentType);

public record VatByDocumentType(string DocumentType, int Count, string? LatestStatus);

public class VatReportQueryHandler : IRequestHandler<VatReportQuery, VatReport>
{
    private readonly IAppDbContext _dbContext;
    public VatReportQueryHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<VatReport> Handle(VatReportQuery request, CancellationToken ct)
    {
        var periodStart = new DateTimeOffset(request.Year, request.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var periodEnd = periodStart.AddMonths(1);

        var docs = _dbContext.FiscalDocuments
            .Where(d => d.CreatedAt >= periodStart && d.CreatedAt < periodEnd);

        var total = await docs.CountAsync(ct);
        var submitted = await docs.CountAsync(d => d.Status == FiscalDocumentStatus.Submitted || d.Status == FiscalDocumentStatus.Confirmed, ct);
        var pending = await docs.CountAsync(d => d.Status == FiscalDocumentStatus.Queued || d.Status == FiscalDocumentStatus.Pending, ct);
        var failed = await docs.CountAsync(d => d.Status == FiscalDocumentStatus.Failed || d.Status == FiscalDocumentStatus.Rejected, ct);

        // Materialize first, then group client-side to avoid provider-specific
        // DateTimeOffset translation issues in OrderByDescending within GroupBy.
        var allDocs = await docs.ToListAsync(ct);

        var byType = allDocs
            .GroupBy(d => d.DocumentType)
            .Select(g => new VatByDocumentType(
                g.Key.ToString(),
                g.Count(),
                g.OrderByDescending(d => d.CreatedAt).FirstOrDefault()?.Status.ToString()))
            .ToList();

        var declaration = await _dbContext.VatDeclarations
            .Where(v => v.PeriodStart >= periodStart && v.PeriodEnd < periodEnd)
            .FirstOrDefaultAsync(ct);

        return new VatReport(
            $"{request.Year:D4}-{request.Month:D2}",
            declaration?.TotalOutputVat ?? 0,
            declaration?.TotalInputVat ?? 0,
            declaration?.NetVat ?? 0,
            total, submitted, pending, failed, byType);
    }
}
