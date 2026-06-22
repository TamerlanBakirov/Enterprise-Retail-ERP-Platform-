using System.Windows;

namespace GeorgiaERP.Desktop.Views.Compliance;

public partial class WaybillEditWindow : Window
{
    public WaybillEditWindow() { InitializeComponent(); }
    private void OnCancel(object sender, RoutedEventArgs e) => Close();
}
