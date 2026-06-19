using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeorgiaERP.Desktop.Models;
using GeorgiaERP.Desktop.Services;

namespace GeorgiaERP.Desktop.ViewModels;

public partial class FinanceViewModel : ObservableObject
{
    private readonly IFinanceService _financeService;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private string _activeTab = "Accounts";
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _totalPages = 1;

    public ObservableCollection<AccountDto> Accounts { get; } = [];
    public ObservableCollection<JournalEntryDto> JournalEntries { get; } = [];
    public ObservableCollection<BankAccountDto> BankAccounts { get; } = [];

    public FinanceViewModel(IFinanceService financeService)
    {
        _financeService = financeService;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            switch (ActiveTab)
            {
                case "Accounts":
                    var accounts = await _financeService.GetAccountsAsync();
                    Accounts.Clear();
                    foreach (var a in accounts) Accounts.Add(a);
                    break;
                case "JournalEntries":
                    var entries = await _financeService.GetJournalEntriesAsync(page: CurrentPage);
                    JournalEntries.Clear();
                    foreach (var e in entries.Items) JournalEntries.Add(e);
                    TotalPages = entries.TotalPages;
                    break;
                case "BankAccounts":
                    var banks = await _financeService.GetBankAccountsAsync();
                    BankAccounts.Clear();
                    foreach (var b in banks) BankAccounts.Add(b);
                    break;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SwitchTabAsync(string tab)
    {
        ActiveTab = tab;
        CurrentPage = 1;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task PostJournalEntryAsync(JournalEntryDto entry)
    {
        var result = await _financeService.PostJournalEntryAsync(entry.Id);
        if (result.IsSuccess) await LoadAsync();
        else ErrorMessage = result.Error;
    }
}
