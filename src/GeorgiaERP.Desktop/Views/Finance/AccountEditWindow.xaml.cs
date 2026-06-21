using System.Windows;

namespace GeorgiaERP.Desktop.Views.Finance;

public partial class AccountEditWindow : Window
{
    public AccountEditWindow()
    {
        InitializeComponent();
    }

    private void OnCancel(object sender, RoutedEventArgs e) => Close();
}
