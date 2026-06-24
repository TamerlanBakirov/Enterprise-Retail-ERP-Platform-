using GeorgiaERP.Application.Common;
using GeorgiaERP.Domain.CRM;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.CRM.Commands;

/// <summary>
/// Expires loyalty points for customers inactive for longer than the given
/// number of months. Inactivity is measured from a customer's most recent
/// loyalty transaction. Without per-lot (FIFO) point tracking, the whole
/// balance is expired once a customer crosses the inactivity threshold — the
/// common model for small/mid retail programs. Writes an Expire ledger row per
/// affected customer so the history remains auditable.
/// </summary>
public record ExpireLoyaltyPointsCommand(int InactivityMonths = 12)
    : IRequest<Result<ExpireLoyaltyPointsResult>>;

public record ExpireLoyaltyPointsResult(int CustomersAffected, int PointsExpired);

public class ExpireLoyaltyPointsCommandHandler
    : IRequestHandler<ExpireLoyaltyPointsCommand, Result<ExpireLoyaltyPointsResult>>
{
    private readonly IAppDbContext _dbContext;
    public ExpireLoyaltyPointsCommandHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<ExpireLoyaltyPointsResult>> Handle(ExpireLoyaltyPointsCommand request, CancellationToken ct)
    {
        if (request.InactivityMonths < 1)
            return Result.Failure<ExpireLoyaltyPointsResult>("InactivityMonths must be at least 1.");

        var cutoff = DateTimeOffset.UtcNow.AddMonths(-request.InactivityMonths);

        var candidateIds = await _dbContext.Customers
            .Where(c => c.IsActive && c.LoyaltyPoints > 0)
            .Select(c => c.Id)
            .ToListAsync(ct);

        if (candidateIds.Count == 0)
            return Result.Success(new ExpireLoyaltyPointsResult(0, 0));

        // Materialize then group client-side: SQLite cannot aggregate Max over a
        // DateTimeOffset in a GROUP BY. The set is bounded to customers with a
        // positive balance.
        var activity = await _dbContext.LoyaltyTransactions
            .Where(t => candidateIds.Contains(t.CustomerId))
            .Select(t => new { t.CustomerId, t.CreatedAt })
            .ToListAsync(ct);

        var lastActivity = activity
            .GroupBy(a => a.CustomerId)
            .ToDictionary(g => g.Key, g => g.Max(x => x.CreatedAt));

        var toExpireIds = candidateIds
            .Where(id => lastActivity.TryGetValue(id, out var last) && last < cutoff)
            .ToList();

        if (toExpireIds.Count == 0)
            return Result.Success(new ExpireLoyaltyPointsResult(0, 0));

        var customers = await _dbContext.Customers
            .Where(c => toExpireIds.Contains(c.Id))
            .ToListAsync(ct);

        var totalExpired = 0;
        foreach (var customer in customers)
        {
            var balance = customer.LoyaltyPoints;
            customer.DeductPoints(balance);
            var tx = LoyaltyTransaction.Create(
                customer.Id, LoyaltyTransactionType.Expire,
                -balance, customer.LoyaltyPoints,
                $"Points expired after {request.InactivityMonths} months of inactivity");
            _dbContext.LoyaltyTransactions.Add(tx);
            totalExpired += balance;
        }

        await _dbContext.SaveChangesAsync(ct);

        return Result.Success(new ExpireLoyaltyPointsResult(customers.Count, totalExpired));
    }
}
