using System.Windows;

namespace GeorgiaERP.Desktop.Views.Inventory;

public partial class TransferOrderEditWindow : Window
{
    public TransferOrderEditWindow()
    {
        InitializeComponent();
    }

    private void OnCancel(object sender, RoutedEventArgs e) => Close();
}
