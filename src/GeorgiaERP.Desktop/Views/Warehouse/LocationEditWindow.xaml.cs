using System.Windows;

namespace GeorgiaERP.Desktop.Views.Warehouse;

public partial class LocationEditWindow : Window
{
    public LocationEditWindow() { InitializeComponent(); }
    private void OnCancel(object sender, RoutedEventArgs e) => Close();
}
