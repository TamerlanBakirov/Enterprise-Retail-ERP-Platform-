using System.Windows;

namespace GeorgiaERP.Desktop.Views.Warehouse;

public partial class ShippingOrderEditWindow : Window
{
    public ShippingOrderEditWindow() { InitializeComponent(); }
    private void OnCancel(object sender, RoutedEventArgs e) => Close();
}
