using System.Windows;

namespace GeorgiaERP.Desktop.Views.Finance;

public partial class BankAccountEditWindow : Window
{
    public BankAccountEditWindow()
    {
        InitializeComponent();
    }

    private void OnCancel(object sender, RoutedEventArgs e) => Close();
}
