using GeorgiaERP.Application.Common;
using GeorgiaERP.Application.Reports.DTOs;
using GeorgiaERP.Domain.Reports;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Reports.Commands;

public record UpdateScheduledReportCommand(
    Guid Id,
    string Name,
    ReportType ReportType,
    string CronExpression,
    string Recipients,
    ScheduleFormat Format) : IRequest<Result<ScheduledReportDto>>;

public class UpdateScheduledReportCommandHandler : IRequestHandler<UpdateScheduledReportCommand, Result<ScheduledReportDto>>
{
    private readonly IAppDbContext _dbContext;

    public UpdateScheduledReportCommandHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<ScheduledReportDto>> Handle(UpdateScheduledReportCommand request, CancellationToken cancellationToken)
    {
        var report = await _dbContext.ScheduledReports
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

        if (report is null)
            return Result.NotFound<ScheduledReportDto>("ScheduledReport", request.Id);

        var nameConflict = await _dbContext.ScheduledReports
            .AnyAsync(r => r.Name == request.Name && r.IsActive && r.Id != request.Id, cancellationToken);

        if (nameConflict)
            return Result.Conflict<ScheduledReportDto>($"A scheduled report with name '{request.Name}' already exists.");

        report.Update(
            name: request.Name,
            reportType: request.ReportType,
            cronExpression: request.CronExpression,
            recipients: request.Recipients,
            format: request.Format);

        await _dbContext.SaveChangesAsync(cancellationToken);

        var dto = new ScheduledReportDto(
            report.Id,
            report.Name,
            report.ReportType,
            report.CronExpression,
            report.Recipients,
            report.Format,
            report.IsActive,
            report.LastRunAt,
            report.NextRunAt,
            report.CreatedAt,
            report.CreatedBy);

        return Result.Success(dto);
    }
}
