using GeorgiaERP.Application.Common;
using GeorgiaERP.Application.Reports.DTOs;
using GeorgiaERP.Domain.Reports;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Reports.Queries;

public record GetScheduledReportsQuery(
    int Page = 1,
    int PageSize = 20,
    bool? IsActive = null) : IRequest<PagedResult<ScheduledReportDto>>;

public class GetScheduledReportsQueryHandler : IRequestHandler<GetScheduledReportsQuery, PagedResult<ScheduledReportDto>>
{
    private readonly IAppDbContext _dbContext;

    public GetScheduledReportsQueryHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<ScheduledReportDto>> Handle(GetScheduledReportsQuery request, CancellationToken cancellationToken)
    {
        var query = _dbContext.ScheduledReports.AsNoTracking();

        if (request.IsActive.HasValue)
            query = query.Where(r => r.IsActive == request.IsActive.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(r => r.Name)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => new ScheduledReportDto(
                r.Id,
                r.Name,
                r.ReportType,
                r.CronExpression,
                r.Recipients,
                r.Format,
                r.IsActive,
                r.LastRunAt,
                r.NextRunAt,
                r.CreatedAt,
                r.CreatedBy))
            .ToListAsync(cancellationToken);

        return new PagedResult<ScheduledReportDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}
