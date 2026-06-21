using System.Windows;

namespace GeorgiaERP.Desktop.Views.Procurement;

public partial class SupplierEditWindow : Window
{
    public SupplierEditWindow()
    {
        InitializeComponent();
    }

    private void OnCancel(object sender, RoutedEventArgs e) => Close();
}
