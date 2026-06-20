using GeorgiaERP.Application.Common;
using GeorgiaERP.Domain.POS;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.POS.Commands;

public record ClosePosSessionCommand(
    Guid SessionId,
    decimal ClosingBalance,
    string? Notes = null) : IRequest<Result<ClosePosSessionResponse>>;

public record ClosePosSessionResponse(
    Guid SessionId,
    decimal OpeningBalance,
    decimal ClosingBalance,
    decimal ExpectedBalance,
    decimal CashDifference,
    int TransactionCount,
    decimal TotalSales);

public class ClosePosSessionCommandHandler
    : IRequestHandler<ClosePosSessionCommand, Result<ClosePosSessionResponse>>
{
    private readonly IAppDbContext _dbContext;

    public ClosePosSessionCommandHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<ClosePosSessionResponse>> Handle(
        ClosePosSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await _dbContext.PosSessions
            .FirstOrDefaultAsync(s => s.Id == request.SessionId, cancellationToken);

        if (session is null)
            return Result.Failure<ClosePosSessionResponse>("Session not found.");

        if (session.Status != PosSessionStatus.Open)
            return Result.Failure<ClosePosSessionResponse>("Session is not open.");

        var transactions = await _dbContext.PosTransactions
            .Where(t => t.SessionId == request.SessionId && t.Status == PosTransactionStatus.Completed)
            .ToListAsync(cancellationToken);

        var totalSales = transactions.Sum(t => t.Total);

        var cashPayments = await _dbContext.PosPayments
            .Where(p => transactions.Select(t => t.Id).Contains(p.TransactionId)
                        && p.PaymentMethod == PaymentMethod.Cash)
            .SumAsync(p => p.Amount - (p.ChangeAmount ?? 0), cancellationToken);

        var expectedBalance = session.OpeningBalance + cashPayments;

        session.Close(request.ClosingBalance, expectedBalance);
        if (request.Notes is not null)
            session.SetNotes(request.Notes);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(new ClosePosSessionResponse(
            session.Id,
            session.OpeningBalance,
            request.ClosingBalance,
            expectedBalance,
            request.ClosingBalance - expectedBalance,
            transactions.Count,
            totalSales));
    }
}
