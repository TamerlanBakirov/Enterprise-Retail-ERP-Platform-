using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeorgiaERP.Desktop.Models;
using GeorgiaERP.Desktop.Services;

namespace GeorgiaERP.Desktop.ViewModels;

public partial class AccountEditViewModel : DialogViewModel
{
    private readonly IFinanceService _financeService;

    [ObservableProperty] private string _code = string.Empty;
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string? _nameKa;
    [ObservableProperty] private string _accountType = "Asset";
    [ObservableProperty] private Guid? _parentId;

    public ObservableCollection<AccountDto> ParentAccounts { get; } = [];

    public AccountEditViewModel(IFinanceService financeService)
    {
        _financeService = financeService;
    }

    public async Task LoadAccountsAsync()
    {
        try
        {
            var accounts = await _financeService.GetAccountsAsync();
            ReplaceItems(ParentAccounts, accounts);
        }
        catch { }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Code) || string.IsNullOrWhiteSpace(Name))
        {
            ErrorMessage = "Code and Name are required.";
            return;
        }

        ErrorMessage = null;
        try
        {
            var request = new CreateAccountRequest(Code, Name, NameKa, AccountType, ParentId);
            var result = await _financeService.CreateAccountAsync(request);
            if (result is not null)
                SaveAndClose();
            else
                ErrorMessage = "Failed to save account.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }
}
