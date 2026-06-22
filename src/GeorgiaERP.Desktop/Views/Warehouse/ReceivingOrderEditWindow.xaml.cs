using System.Windows;

namespace GeorgiaERP.Desktop.Views.Warehouse;

public partial class ReceivingOrderEditWindow : Window
{
    public ReceivingOrderEditWindow() { InitializeComponent(); }
    private void OnCancel(object sender, RoutedEventArgs e) => Close();
}
