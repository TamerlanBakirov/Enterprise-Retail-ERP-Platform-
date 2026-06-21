using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeorgiaERP.Desktop.Services;

namespace GeorgiaERP.Desktop.ViewModels;

public partial class TwoFactorSetupViewModel : DialogViewModel
{
    private readonly IAuthService _authService;

    [ObservableProperty] private string? _sharedKey;
    [ObservableProperty] private string _code = string.Empty;
    [ObservableProperty] private bool _isSetupMode = true;
    [ObservableProperty] private bool _setupStarted;
    [ObservableProperty] private string? _successMessage;

    public TwoFactorSetupViewModel(IAuthService authService)
    {
        _authService = authService;
    }

    [RelayCommand]
    private async Task BeginSetupAsync()
    {
        await ExecuteAsync(async () =>
        {
            var result = await _authService.BeginTwoFactorSetupAsync();
            if (result is not null)
            {
                SharedKey = result.SharedKey;
                SetupStarted = true;
            }
            else
            {
                ErrorMessage = "Failed to begin 2FA setup.";
            }
        });
    }

    [RelayCommand]
    private async Task ConfirmSetupAsync()
    {
        if (string.IsNullOrWhiteSpace(Code))
        {
            ErrorMessage = "Enter the 6-digit code from your authenticator app.";
            return;
        }

        await ExecuteAsync(async () =>
        {
            var (success, error) = await _authService.ConfirmTwoFactorSetupAsync(Code);
            if (success)
            {
                SuccessMessage = "Two-factor authentication enabled successfully.";
                Saved = true;
            }
            else
            {
                ErrorMessage = error ?? "Invalid code.";
            }
        });
    }

    [RelayCommand]
    private async Task DisableAsync()
    {
        if (string.IsNullOrWhiteSpace(Code))
        {
            ErrorMessage = "Enter your current 2FA code to disable.";
            return;
        }

        await ExecuteAsync(async () =>
        {
            var (success, error) = await _authService.DisableTwoFactorAsync(Code);
            if (success)
            {
                SuccessMessage = "Two-factor authentication disabled.";
                Saved = true;
            }
            else
            {
                ErrorMessage = error ?? "Invalid code.";
            }
        });
    }
}
