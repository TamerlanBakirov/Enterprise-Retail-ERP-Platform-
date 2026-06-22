using System.Windows;

namespace GeorgiaERP.Desktop.Views.Warehouse;

public partial class WarehouseEditWindow : Window
{
    public WarehouseEditWindow() { InitializeComponent(); }
    private void OnCancel(object sender, RoutedEventArgs e) => Close();
}
