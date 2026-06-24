using GeorgiaERP.Application.Common;
using GeorgiaERP.Domain.CRM;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.CRM.Commands;

/// <summary>
/// Recomputes the loyalty tier of every active customer from their lifetime
/// spend using <see cref="LoyaltyTierPolicy"/> and persists any changes. Run by
/// an admin or scheduler after bulk purchase imports or threshold changes.
/// </summary>
public record RecalculateLoyaltyTiersCommand : IRequest<Result<RecalculateLoyaltyTiersResult>>;

public record RecalculateLoyaltyTiersResult(int CustomersEvaluated, int TiersChanged);

public class RecalculateLoyaltyTiersCommandHandler
    : IRequestHandler<RecalculateLoyaltyTiersCommand, Result<RecalculateLoyaltyTiersResult>>
{
    private readonly IAppDbContext _dbContext;
    public RecalculateLoyaltyTiersCommandHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<RecalculateLoyaltyTiersResult>> Handle(RecalculateLoyaltyTiersCommand request, CancellationToken ct)
    {
        var customers = await _dbContext.Customers
            .Where(c => c.IsActive)
            .ToListAsync(ct);

        var changed = 0;
        foreach (var customer in customers)
        {
            var tier = LoyaltyTierPolicy.ForSpend(customer.TotalPurchases);
            if (customer.LoyaltyTier != tier)
            {
                customer.AssignLoyaltyTier(tier);
                changed++;
            }
        }

        if (changed > 0)
            await _dbContext.SaveChangesAsync(ct);

        return Result.Success(new RecalculateLoyaltyTiersResult(customers.Count, changed));
    }
}
