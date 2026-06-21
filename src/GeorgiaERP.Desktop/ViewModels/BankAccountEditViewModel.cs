using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeorgiaERP.Desktop.Models;
using GeorgiaERP.Desktop.Services;

namespace GeorgiaERP.Desktop.ViewModels;

public partial class BankAccountEditViewModel : DialogViewModel
{
    private readonly IFinanceService _financeService;

    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _bankName = string.Empty;
    [ObservableProperty] private string _accountNumber = string.Empty;
    [ObservableProperty] private string? _iban;
    [ObservableProperty] private string _currency = "GEL";
    [ObservableProperty] private decimal _initialBalance;
    [ObservableProperty] private Guid? _glAccountId;

    public ObservableCollection<AccountDto> GlAccounts { get; } = [];

    public BankAccountEditViewModel(IFinanceService financeService)
    {
        _financeService = financeService;
    }

    public async Task LoadAccountsAsync()
    {
        try
        {
            var accounts = await _financeService.GetAccountsAsync(isActive: true);
            ReplaceItems(GlAccounts, accounts);
        }
        catch { }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(BankName) || string.IsNullOrWhiteSpace(AccountNumber))
        {
            ErrorMessage = "Name, Bank Name, and Account Number are required.";
            return;
        }

        ErrorMessage = null;
        try
        {
            var request = new CreateBankAccountRequest(Name, BankName, AccountNumber, Iban, Currency, InitialBalance, GlAccountId);
            var result = await _financeService.CreateBankAccountAsync(request);
            if (result is not null)
                SaveAndClose();
            else
                ErrorMessage = "Failed to save bank account.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }
}
