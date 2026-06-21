using System.Windows;

namespace GeorgiaERP.Desktop.Views.Pricing;

public partial class SetPriceWindow : Window
{
    public SetPriceWindow() { InitializeComponent(); }
    private void OnCancel(object sender, RoutedEventArgs e) => Close();
}
