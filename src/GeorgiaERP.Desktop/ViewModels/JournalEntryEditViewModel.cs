using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeorgiaERP.Desktop.Models;
using GeorgiaERP.Desktop.Services;

namespace GeorgiaERP.Desktop.ViewModels;

public partial class JournalEntryEditViewModel : DialogViewModel
{
    private readonly IFinanceService _financeService;

    [ObservableProperty] private string _description = string.Empty;
    [ObservableProperty] private DateTimeOffset _entryDate = DateTimeOffset.Now;

    public ObservableCollection<AccountDto> Accounts { get; } = [];
    public ObservableCollection<JournalLineViewModel> Lines { get; } = [];

    public JournalEntryEditViewModel(IFinanceService financeService)
    {
        _financeService = financeService;
        Lines.Add(new JournalLineViewModel());
        Lines.Add(new JournalLineViewModel());
    }

    public async Task LoadAccountsAsync()
    {
        try
        {
            var accounts = await _financeService.GetAccountsAsync(isActive: true);
            ReplaceItems(Accounts, accounts);
        }
        catch { }
    }

    [RelayCommand]
    private void AddLine() => Lines.Add(new JournalLineViewModel());

    [RelayCommand]
    private void RemoveLine(JournalLineViewModel line)
    {
        if (Lines.Count > 2) Lines.Remove(line);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Description))
        {
            ErrorMessage = "Description is required.";
            return;
        }

        var totalDebit = Lines.Sum(l => l.DebitAmount);
        var totalCredit = Lines.Sum(l => l.CreditAmount);
        if (totalDebit != totalCredit || totalDebit == 0)
        {
            ErrorMessage = "Total debits must equal total credits and be greater than zero.";
            return;
        }

        if (Lines.Any(l => l.AccountId is null))
        {
            ErrorMessage = "All lines must have an account selected.";
            return;
        }

        ErrorMessage = null;
        try
        {
            var lines = Lines.Select(l => new JournalEntryLineRequest(
                l.AccountId!.Value, l.DebitAmount, l.CreditAmount, l.Description)).ToList();

            var request = new CreateJournalEntryRequest(Description, EntryDate, lines);
            var result = await _financeService.CreateJournalEntryAsync(request);
            if (result is not null)
                SaveAndClose();
            else
                ErrorMessage = "Failed to save journal entry.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }
}

public partial class JournalLineViewModel : ObservableObject
{
    [ObservableProperty] private Guid? _accountId;
    [ObservableProperty] private decimal _debitAmount;
    [ObservableProperty] private decimal _creditAmount;
    [ObservableProperty] private string? _description;
}
