using GeorgiaERP.Application.Common;
using GeorgiaERP.Domain.Finance;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Finance.Commands;

public record CreateAccountCommand(
    string AccountCode,
    string Name,
    string? NameKa,
    string AccountType,
    string BalanceType,
    Guid? ParentId,
    bool IsHeader) : IRequest<Result<Guid>>;

public class CreateAccountCommandHandler : IRequestHandler<CreateAccountCommand, Result<Guid>>
{
    private readonly IAppDbContext _dbContext;
    public CreateAccountCommandHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<Guid>> Handle(CreateAccountCommand request, CancellationToken ct)
    {
        var exists = await _dbContext.ChartOfAccounts.AnyAsync(a => a.AccountCode == request.AccountCode, ct);
        if (exists) return Result.Failure<Guid>($"Account code '{request.AccountCode}' already exists.");

        if (!Enum.TryParse<AccountType>(request.AccountType, true, out var acctType))
            return Result.Failure<Guid>("Invalid account type.");
        if (!Enum.TryParse<BalanceType>(request.BalanceType, true, out var balType))
            return Result.Failure<Guid>("Invalid balance type.");

        if (request.ParentId.HasValue)
        {
            var parentExists = await _dbContext.ChartOfAccounts.AnyAsync(a => a.Id == request.ParentId.Value, ct);
            if (!parentExists) return Result.Failure<Guid>("Parent account not found.");
        }

        var account = ChartOfAccount.Create(request.AccountCode, request.Name, acctType, balType,
            request.NameKa, request.ParentId, request.IsHeader);

        _dbContext.ChartOfAccounts.Add(account);
        await _dbContext.SaveChangesAsync(ct);
        return Result.Success(account.Id);
    }
}
