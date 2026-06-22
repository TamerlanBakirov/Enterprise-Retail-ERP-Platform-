using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using GeorgiaERP.Desktop.Models;
using GeorgiaERP.Desktop.Services;

namespace GeorgiaERP.Desktop.ViewModels;

public partial class FinanceViewModel : TabbedPagedViewModel
{
    private readonly IFinanceService _financeService;

    public ObservableCollection<AccountDto> Accounts { get; } = [];
    public ObservableCollection<JournalEntryDto> JournalEntries { get; } = [];
    public ObservableCollection<BankAccountDto> BankAccounts { get; } = [];

    public FinanceViewModel(IFinanceService financeService)
    {
        _financeService = financeService;
        ActiveTab = "Accounts";
    }

    protected override async Task LoadCoreAsync()
    {
        switch (ActiveTab)
        {
            case "Accounts":
                var accounts = await _financeService.GetAccountsAsync();
                ReplaceItems(Accounts, accounts);
                break;
            case "JournalEntries":
                var entries = await _financeService.GetJournalEntriesAsync(page: CurrentPage);
                ReplaceItems(JournalEntries, entries.Items);
                TotalPages = entries.TotalPages;
                break;
            case "BankAccounts":
                var banks = await _financeService.GetBankAccountsAsync();
                ReplaceItems(BankAccounts, banks);
                break;
        }
    }

    [RelayCommand]
    private async Task PostJournalEntryAsync(JournalEntryDto entry)
    {
        var result = await _financeService.PostJournalEntryAsync(entry.Id);
        if (result.IsSuccess) await LoadAsync();
        else ErrorMessage = result.Error;
    }
}
