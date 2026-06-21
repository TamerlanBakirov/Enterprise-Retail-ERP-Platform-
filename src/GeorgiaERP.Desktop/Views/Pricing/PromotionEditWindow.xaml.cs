using System.Windows;

namespace GeorgiaERP.Desktop.Views.Pricing;

public partial class PromotionEditWindow : Window
{
    public PromotionEditWindow() { InitializeComponent(); }
    private void OnCancel(object sender, RoutedEventArgs e) => Close();
}
