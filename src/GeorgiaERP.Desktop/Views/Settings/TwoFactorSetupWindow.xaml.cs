using System.Windows;

namespace GeorgiaERP.Desktop.Views.Settings;

public partial class TwoFactorSetupWindow : Window
{
    public TwoFactorSetupWindow() { InitializeComponent(); }
    private void OnClose(object sender, RoutedEventArgs e) => Close();
}
