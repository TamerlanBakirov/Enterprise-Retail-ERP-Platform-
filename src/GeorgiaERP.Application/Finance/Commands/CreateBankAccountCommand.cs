using GeorgiaERP.Application.Common;
using GeorgiaERP.Domain.Finance;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Finance.Commands;

public record CreateBankAccountCommand(
    string AccountName,
    string BankName,
    string AccountNumber,
    string? Iban,
    string Currency,
    Guid? GlAccountId) : IRequest<Result<Guid>>, ICacheInvalidator
{
    public IReadOnlyList<string> CacheKeysToInvalidate => ["finance:bank-accounts", "dashboard:kpi"];
}

public class CreateBankAccountCommandHandler : IRequestHandler<CreateBankAccountCommand, Result<Guid>>
{
    private readonly IAppDbContext _dbContext;
    public CreateBankAccountCommandHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<Guid>> Handle(CreateBankAccountCommand request, CancellationToken ct)
    {
        var exists = await _dbContext.BankAccounts.AnyAsync(a => a.AccountNumber == request.AccountNumber, ct);
        if (exists) return Result.Failure<Guid>("Account number already exists.");

        var account = BankAccount.Create(request.AccountName, request.BankName, request.AccountNumber, request.Currency);
        if (request.Iban is not null) account.SetIban(request.Iban);
        if (request.GlAccountId.HasValue) account.LinkGlAccount(request.GlAccountId.Value);

        _dbContext.BankAccounts.Add(account);
        await _dbContext.SaveChangesAsync(ct);
        return Result.Success(account.Id);
    }
}
