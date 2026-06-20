using GeorgiaERP.Desktop.Models;

namespace GeorgiaERP.Desktop.Services;

public interface IFinanceService
{
    Task<List<AccountDto>> GetAccountsAsync(bool? isActive = null);
    Task<AccountDto?> CreateAccountAsync(CreateAccountRequest request);
    Task<PagedResult<JournalEntryDto>> GetJournalEntriesAsync(string? status = null, int page = 1, int pageSize = 20);
    Task<JournalEntryDto?> CreateJournalEntryAsync(CreateJournalEntryRequest request);
    Task<ApiResult> PostJournalEntryAsync(Guid id);
    Task<List<BankAccountDto>> GetBankAccountsAsync();
    Task<BankAccountDto?> CreateBankAccountAsync(CreateBankAccountRequest request);
}

public class FinanceService : IFinanceService
{
    private readonly IApiClient _api;
    public FinanceService(IApiClient api) => _api = api;

    public Task<List<AccountDto>> GetAccountsAsync(bool? isActive)
    {
        var q = "finance/chart-of-accounts";
        if (isActive.HasValue) q += $"?isActive={isActive}";
        return _api.GetAsync<List<AccountDto>>(q)!;
    }

    public Task<AccountDto?> CreateAccountAsync(CreateAccountRequest request) =>
        _api.PostAsync<CreateAccountRequest, AccountDto>("finance/chart-of-accounts", request);

    public Task<PagedResult<JournalEntryDto>> GetJournalEntriesAsync(string? status, int page, int pageSize)
    {
        var q = $"finance/journal-entries?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(status)) q += $"&status={status}";
        return _api.GetAsync<PagedResult<JournalEntryDto>>(q)!;
    }

    public Task<JournalEntryDto?> CreateJournalEntryAsync(CreateJournalEntryRequest request) =>
        _api.PostAsync<CreateJournalEntryRequest, JournalEntryDto>("finance/journal-entries", request);

    public Task<ApiResult> PostJournalEntryAsync(Guid id) => _api.PostAsync($"finance/journal-entries/{id}/post");

    public Task<List<BankAccountDto>> GetBankAccountsAsync() => _api.GetAsync<List<BankAccountDto>>("finance/bank-accounts")!;

    public Task<BankAccountDto?> CreateBankAccountAsync(CreateBankAccountRequest request) =>
        _api.PostAsync<CreateBankAccountRequest, BankAccountDto>("finance/bank-accounts", request);
}
