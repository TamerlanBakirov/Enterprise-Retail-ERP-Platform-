using GeorgiaERP.Application.Common;
using GeorgiaERP.Domain.CRM;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.CRM.Commands;

public record EarnLoyaltyPointsCommand(
    Guid CustomerId,
    int Points,
    string? ReferenceType = null,
    Guid? ReferenceId = null,
    string? Description = null) : IRequest<Result<int>>;

public record RedeemLoyaltyPointsCommand(
    Guid CustomerId,
    int Points,
    string? Description = null) : IRequest<Result<int>>;

public class EarnLoyaltyPointsCommandHandler : IRequestHandler<EarnLoyaltyPointsCommand, Result<int>>
{
    private readonly IAppDbContext _dbContext;
    public EarnLoyaltyPointsCommandHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<int>> Handle(EarnLoyaltyPointsCommand request, CancellationToken ct)
    {
        var customer = await _dbContext.Customers.FirstOrDefaultAsync(c => c.Id == request.CustomerId && c.IsActive, ct);
        if (customer is null) return Result.Failure<int>("Customer not found.");

        customer.AddPoints(request.Points);
        var tx = LoyaltyTransaction.Create(customer.Id, LoyaltyTransactionType.Earn,
            request.Points, customer.LoyaltyPoints, request.Description);
        tx.SetReference(request.ReferenceType, request.ReferenceId);

        _dbContext.LoyaltyTransactions.Add(tx);
        await _dbContext.SaveChangesAsync(ct);

        return Result.Success(customer.LoyaltyPoints);
    }
}

public class RedeemLoyaltyPointsCommandHandler : IRequestHandler<RedeemLoyaltyPointsCommand, Result<int>>
{
    private readonly IAppDbContext _dbContext;
    public RedeemLoyaltyPointsCommandHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<int>> Handle(RedeemLoyaltyPointsCommand request, CancellationToken ct)
    {
        var customer = await _dbContext.Customers.FirstOrDefaultAsync(c => c.Id == request.CustomerId && c.IsActive, ct);
        if (customer is null) return Result.Failure<int>("Customer not found.");
        if (customer.LoyaltyPoints < request.Points)
            return Result.Failure<int>($"Insufficient points: {customer.LoyaltyPoints} available, {request.Points} requested.");

        customer.DeductPoints(request.Points);
        var tx = LoyaltyTransaction.Create(customer.Id, LoyaltyTransactionType.Redeem,
            -request.Points, customer.LoyaltyPoints, request.Description);

        _dbContext.LoyaltyTransactions.Add(tx);
        await _dbContext.SaveChangesAsync(ct);

        return Result.Success(customer.LoyaltyPoints);
    }
}
