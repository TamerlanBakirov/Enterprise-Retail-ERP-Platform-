using System.Windows.Controls;
using System.Windows.Input;
using GeorgiaERP.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace GeorgiaERP.Desktop.Views.POS;

public partial class PosView : UserControl
{
    public PosView()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<PosViewModel>();
    }

    private void OnBarcodeKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is PosViewModel vm)
        {
            vm.ScanBarcodeCommand.Execute(null);
            e.Handled = true;
        }
    }
}
