using System.Windows;

namespace GeorgiaERP.Desktop.Views.Pricing;

public partial class PriceListEditWindow : Window
{
    public PriceListEditWindow() { InitializeComponent(); }
    private void OnCancel(object sender, RoutedEventArgs e) => Close();
}
