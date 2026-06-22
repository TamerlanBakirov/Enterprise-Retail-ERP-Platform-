using System.Windows;
using System.Windows.Controls;
using GeorgiaERP.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace GeorgiaERP.Desktop.Views.Customers;

public partial class CustomersView : UserControl
{
    private readonly CustomersViewModel _viewModel;

    public CustomersView()
    {
        InitializeComponent();
        _viewModel = App.Services.GetRequiredService<CustomersViewModel>();
        DataContext = _viewModel;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        IsVisibleChanged += OnVisibilityChanged;
        if (Visibility == Visibility.Visible && _viewModel.Customers.Count == 0)
            await _viewModel.LoadCommand.ExecuteAsync(null);
    }

    private async void OnVisibilityChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is Visibility v && v == Visibility.Visible && _viewModel.Customers.Count == 0)
            await _viewModel.LoadCommand.ExecuteAsync(null);
    }

    private async void OnAddCustomer(object sender, RoutedEventArgs e)
    {
        var vm = App.Services.GetRequiredService<CustomerEditViewModel>();
        var window = new CustomerEditWindow { DataContext = vm, Owner = Window.GetWindow(this) };
        window.ShowDialog();
        if (vm.Saved)
            await _viewModel.LoadCommand.ExecuteAsync(null);
    }
}
