using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GeorgiaERP.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace GeorgiaERP.Desktop.Views.POS;

public partial class PosView : UserControl
{
    private readonly PosViewModel _viewModel;
    private bool _initialized;

    public PosView()
    {
        InitializeComponent();
        _viewModel = App.Services.GetRequiredService<PosViewModel>();
        DataContext = _viewModel;
        IsVisibleChanged += OnVisibilityChanged;
    }

    private async void OnVisibilityChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is true && !_initialized)
        {
            _initialized = true;
            await _viewModel.InitializeAsync();
        }
    }

    private void OnBarcodeKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is PosViewModel vm && vm.HasSession)
        {
            vm.ScanBarcodeCommand.Execute(null);
            e.Handled = true;
        }
    }
}
