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
        if (_viewModel.Accounts.Count == 0)
            await _viewModel.LoadCommand.ExecuteAsync(null);
    }
}
