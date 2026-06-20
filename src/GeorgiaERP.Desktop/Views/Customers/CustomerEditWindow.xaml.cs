using System.Windows;

namespace GeorgiaERP.Desktop.Views.Customers;

public partial class CustomerEditWindow : Window
{
    public CustomerEditWindow()
    {
        InitializeComponent();
    }

    private void OnCancel(object sender, RoutedEventArgs e) => Close();
}
