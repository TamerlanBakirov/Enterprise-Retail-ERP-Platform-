using System.Windows;
using System.Windows.Controls;
using GeorgiaERP.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace GeorgiaERP.Desktop.Views.Pricing;

public partial class PricingView : UserControl
{
    private readonly PricingViewModel _viewModel;

    public PricingView()
    {
        InitializeComponent();
        _viewModel = App.Services.GetRequiredService<PricingViewModel>();
        DataContext = _viewModel;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        IsVisibleChanged += OnVisibilityChanged;
        if (Visibility == Visibility.Visible && _viewModel.PriceLists.Count == 0)
            await _viewModel.LoadCommand.ExecuteAsync(null);
    }

    private async void OnVisibilityChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is Visibility v && v == Visibility.Visible && _viewModel.PriceLists.Count == 0)
            await _viewModel.LoadCommand.ExecuteAsync(null);
    }

    private async void OnAddPriceList(object sender, RoutedEventArgs e)
    {
        var vm = App.Services.GetRequiredService<PriceListEditViewModel>();
        var window = new PriceListEditWindow { DataContext = vm, Owner = Window.GetWindow(this) };
        window.ShowDialog();
        if (vm.Saved) await _viewModel.LoadCommand.ExecuteAsync(null);
    }

    private async void OnAddPromotion(object sender, RoutedEventArgs e)
    {
        var vm = App.Services.GetRequiredService<PromotionEditViewModel>();
        var window = new PromotionEditWindow { DataContext = vm, Owner = Window.GetWindow(this) };
        window.ShowDialog();
        if (vm.Saved) await _viewModel.LoadCommand.ExecuteAsync(null);
    }

    private async void OnSetPrice(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedPriceList is null)
        {
            MessageBox.Show("Please select a price list first.", "Georgia ERP");
            return;
        }
        var vm = App.Services.GetRequiredService<SetPriceViewModel>();
        vm.PriceListId = _viewModel.SelectedPriceList.Id;
        await vm.LoadProductsAsync(App.Services.GetRequiredService<Services.IProductService>());
        var window = new SetPriceWindow { DataContext = vm, Owner = Window.GetWindow(this) };
        window.ShowDialog();
        if (vm.Saved) await _viewModel.LoadItemsCommand.ExecuteAsync(null);
    }
}
