using System.Windows;
using System.Windows.Controls;
using GeorgiaERP.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace GeorgiaERP.Desktop.Views.Finance;

public partial class FinanceView : UserControl
{
    private readonly FinanceViewModel _viewModel;

    public FinanceView()
    {
        InitializeComponent();
        _viewModel = App.Services.GetRequiredService<FinanceViewModel>();
        DataContext = _viewModel;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        IsVisibleChanged += OnVisibilityChanged;
        if (Visibility == Visibility.Visible && _viewModel.Accounts.Count == 0)
            await _viewModel.LoadCommand.ExecuteAsync(null);
    }

    private async void OnVisibilityChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is true && _viewModel.Accounts.Count == 0)
            await _viewModel.LoadCommand.ExecuteAsync(null);
    }

    private async void OnAddAccount(object sender, RoutedEventArgs e)
    {
        var vm = App.Services.GetRequiredService<AccountEditViewModel>();
        await vm.LoadAccountsAsync();
        var window = new AccountEditWindow { DataContext = vm, Owner = Window.GetWindow(this) };
        window.ShowDialog();
        if (vm.Saved) await _viewModel.LoadCommand.ExecuteAsync(null);
    }

    private async void OnAddJournalEntry(object sender, RoutedEventArgs e)
    {
        var vm = App.Services.GetRequiredService<JournalEntryEditViewModel>();
        await vm.LoadAccountsAsync();
        var window = new JournalEntryEditWindow { DataContext = vm, Owner = Window.GetWindow(this) };
        window.ShowDialog();
        if (vm.Saved) await _viewModel.LoadCommand.ExecuteAsync(null);
    }

    private async void OnAddBankAccount(object sender, RoutedEventArgs e)
    {
        var vm = App.Services.GetRequiredService<BankAccountEditViewModel>();
        await vm.LoadAccountsAsync();
        var window = new BankAccountEditWindow { DataContext = vm, Owner = Window.GetWindow(this) };
        window.ShowDialog();
        if (vm.Saved) await _viewModel.LoadCommand.ExecuteAsync(null);
    }
}
