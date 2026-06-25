using GeorgiaERP.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Reports.Commands;

public record DeleteScheduledReportCommand(Guid Id) : IRequest<Result>;

public class DeleteScheduledReportCommandHandler : IRequestHandler<DeleteScheduledReportCommand, Result>
{
    private readonly IAppDbContext _dbContext;

    public DeleteScheduledReportCommandHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result> Handle(DeleteScheduledReportCommand request, CancellationToken cancellationToken)
    {
        var report = await _dbContext.ScheduledReports
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

        if (report is null)
            return Result.NotFound("ScheduledReport", request.Id);

        report.Deactivate();
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
