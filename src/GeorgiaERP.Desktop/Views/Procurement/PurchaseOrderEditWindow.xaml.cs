using System.Windows;

namespace GeorgiaERP.Desktop.Views.Procurement;

public partial class PurchaseOrderEditWindow : Window
{
    public PurchaseOrderEditWindow()
    {
        InitializeComponent();
    }

    private void OnCancel(object sender, RoutedEventArgs e) => Close();
}
