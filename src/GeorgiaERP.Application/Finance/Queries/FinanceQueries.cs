using GeorgiaERP.Application.Common;
using GeorgiaERP.Application.Finance.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Finance.Queries;

public record GetChartOfAccountsQuery(bool? IsActive = null) : IRequest<IReadOnlyList<ChartOfAccountDto>>;

public record GetChartOfAccountByIdQuery(Guid Id) : IRequest<ChartOfAccountDto?>;

public record GetJournalEntriesQuery(
    string? Status = null,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<JournalEntryDto>>;

public record GetJournalEntryByIdQuery(Guid Id) : IRequest<JournalEntryDetailDto?>;

public record GetBankAccountsQuery() : IRequest<IReadOnlyList<BankAccountDto>>;

public record GetBankAccountByIdQuery(Guid Id) : IRequest<BankAccountDto?>;

public class GetChartOfAccountsQueryHandler : IRequestHandler<GetChartOfAccountsQuery, IReadOnlyList<ChartOfAccountDto>>
{
    private readonly IAppDbContext _dbContext;
    public GetChartOfAccountsQueryHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<IReadOnlyList<ChartOfAccountDto>> Handle(GetChartOfAccountsQuery request, CancellationToken ct)
    {
        var query = _dbContext.ChartOfAccounts.AsNoTracking();

        if (request.IsActive.HasValue)
            query = query.Where(a => a.IsActive == request.IsActive.Value);

        return await query
            .OrderBy(a => a.AccountCode)
            .Select(a => new ChartOfAccountDto(
                a.Id, a.AccountCode, a.Name, a.NameKa,
                a.AccountType.ToString(), a.BalanceType.ToString(),
                a.ParentId, a.IsHeader, a.IsSystem, a.IsActive))
            .ToListAsync(ct);
    }
}

public class GetChartOfAccountByIdQueryHandler : IRequestHandler<GetChartOfAccountByIdQuery, ChartOfAccountDto?>
{
    private readonly IAppDbContext _dbContext;
    public GetChartOfAccountByIdQueryHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<ChartOfAccountDto?> Handle(GetChartOfAccountByIdQuery request, CancellationToken ct)
    {
        return await _dbContext.ChartOfAccounts
            .AsNoTracking()
            .Where(a => a.Id == request.Id)
            .Select(a => new ChartOfAccountDto(
                a.Id, a.AccountCode, a.Name, a.NameKa,
                a.AccountType.ToString(), a.BalanceType.ToString(),
                a.ParentId, a.IsHeader, a.IsSystem, a.IsActive))
            .FirstOrDefaultAsync(ct);
    }
}

public class GetJournalEntriesQueryHandler : IRequestHandler<GetJournalEntriesQuery, PagedResult<JournalEntryDto>>
{
    private readonly IAppDbContext _dbContext;
    public GetJournalEntriesQueryHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<PagedResult<JournalEntryDto>> Handle(GetJournalEntriesQuery request, CancellationToken ct)
    {
        var query = _dbContext.JournalEntries.AsNoTracking();

        if (!string.IsNullOrEmpty(request.Status) &&
            Enum.TryParse<Domain.Finance.JournalEntryStatus>(request.Status, true, out var entryStatus))
            query = query.Where(j => j.Status == entryStatus);

        var totalCount = await query.CountAsync(ct);

        var rawItems = await query.ToListAsync(ct);

        var items = rawItems
            .OrderByDescending(j => j.EntryDate)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(j => new JournalEntryDto(
                j.Id, j.EntryNumber, j.EntryDate, j.Description,
                j.Status.ToString(), j.TotalDebit, j.TotalCredit, j.PostedAt, j.CreatedAt)).ToList();

        return new PagedResult<JournalEntryDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}

public class GetJournalEntryByIdQueryHandler : IRequestHandler<GetJournalEntryByIdQuery, JournalEntryDetailDto?>
{
    private readonly IAppDbContext _dbContext;
    public GetJournalEntryByIdQueryHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<JournalEntryDetailDto?> Handle(GetJournalEntryByIdQuery request, CancellationToken ct)
    {
        var entry = await _dbContext.JournalEntries
            .AsNoTracking()
            .Where(j => j.Id == request.Id)
            .Select(j => new
            {
                j.Id, j.EntryNumber, j.EntryDate, j.Description,
                Status = j.Status.ToString(), j.TotalDebit, j.TotalCredit,
                j.SourceType, j.SourceId, j.PostedAt, j.CreatedBy, j.CreatedAt
            })
            .FirstOrDefaultAsync(ct);

        if (entry is null)
            return null;

        var lines = await _dbContext.JournalEntryLines
            .AsNoTracking()
            .Where(l => l.JournalEntryId == request.Id)
            .Join(_dbContext.ChartOfAccounts,
                l => l.AccountId, a => a.Id,
                (l, a) => new JournalEntryLineDto(
                    l.Id, l.LineNumber, l.AccountId, a.AccountCode, a.Name,
                    l.DebitAmount, l.CreditAmount, l.Description))
            .OrderBy(l => l.LineNumber)
            .ToListAsync(ct);

        return new JournalEntryDetailDto(
            entry.Id, entry.EntryNumber, entry.EntryDate, entry.Description,
            entry.Status, entry.TotalDebit, entry.TotalCredit,
            entry.SourceType, entry.SourceId, entry.PostedAt, entry.CreatedBy, entry.CreatedAt,
            lines);
    }
}

public class GetBankAccountsQueryHandler : IRequestHandler<GetBankAccountsQuery, IReadOnlyList<BankAccountDto>>
{
    private readonly IAppDbContext _dbContext;
    public GetBankAccountsQueryHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<IReadOnlyList<BankAccountDto>> Handle(GetBankAccountsQuery request, CancellationToken ct)
    {
        return await _dbContext.BankAccounts
            .AsNoTracking()
            .OrderBy(a => a.AccountName)
            .Select(a => new BankAccountDto(
                a.Id, a.AccountName, a.BankName, a.AccountNumber,
                a.Iban, a.Currency, a.CurrentBalance, a.GlAccountId, a.IsActive))
            .ToListAsync(ct);
    }
}

public class GetBankAccountByIdQueryHandler : IRequestHandler<GetBankAccountByIdQuery, BankAccountDto?>
{
    private readonly IAppDbContext _dbContext;
    public GetBankAccountByIdQueryHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<BankAccountDto?> Handle(GetBankAccountByIdQuery request, CancellationToken ct)
    {
        return await _dbContext.BankAccounts
            .AsNoTracking()
            .Where(a => a.Id == request.Id)
            .Select(a => new BankAccountDto(
                a.Id, a.AccountName, a.BankName, a.AccountNumber,
                a.Iban, a.Currency, a.CurrentBalance, a.GlAccountId, a.IsActive))
            .FirstOrDefaultAsync(ct);
    }
}
