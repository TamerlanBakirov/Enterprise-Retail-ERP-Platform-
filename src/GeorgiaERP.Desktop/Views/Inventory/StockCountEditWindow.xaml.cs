using System.Windows;

namespace GeorgiaERP.Desktop.Views.Inventory;

public partial class StockCountEditWindow : Window
{
    public StockCountEditWindow()
    {
        InitializeComponent();
    }

    private void OnCancel(object sender, RoutedEventArgs e) => Close();
}
