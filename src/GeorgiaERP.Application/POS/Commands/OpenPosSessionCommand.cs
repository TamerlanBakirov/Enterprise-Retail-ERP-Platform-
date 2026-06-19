using GeorgiaERP.Application.Common;
using GeorgiaERP.Domain.POS;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.POS.Commands;

public record OpenPosSessionCommand(
    Guid TerminalId,
    Guid CashierId,
    decimal OpeningBalance) : IRequest<Result<PosSessionResponse>>;

public record PosSessionResponse(
    Guid SessionId,
    string TerminalCode,
    string Status,
    DateTimeOffset OpenedAt);

public class OpenPosSessionCommandHandler
    : IRequestHandler<OpenPosSessionCommand, Result<PosSessionResponse>>
{
    private readonly IAppDbContext _dbContext;

    public OpenPosSessionCommandHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<PosSessionResponse>> Handle(
        OpenPosSessionCommand request, CancellationToken cancellationToken)
    {
        var terminal = await _dbContext.PosTerminals
            .FirstOrDefaultAsync(t => t.Id == request.TerminalId && t.IsActive, cancellationToken);

        if (terminal is null)
            return Result.Failure<PosSessionResponse>("Terminal not found or inactive.");

        var existingOpen = await _dbContext.PosSessions
            .AnyAsync(s => s.TerminalId == request.TerminalId && s.Status == PosSessionStatus.Open, cancellationToken);

        if (existingOpen)
            return Result.Failure<PosSessionResponse>("Terminal already has an open session. Close it first.");

        var session = PosSession.Create(request.TerminalId, request.CashierId, request.OpeningBalance);

        _dbContext.PosSessions.Add(session);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(new PosSessionResponse(
            session.Id,
            terminal.Code,
            session.Status.ToString(),
            session.OpenedAt));
    }
}
