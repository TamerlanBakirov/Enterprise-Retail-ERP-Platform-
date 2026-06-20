using System.Windows;

namespace GeorgiaERP.Desktop.Views.Products;

public partial class ProductEditWindow : Window
{
    public ProductEditWindow()
    {
        InitializeComponent();
    }

    private void OnCancel(object sender, RoutedEventArgs e) => Close();
}
