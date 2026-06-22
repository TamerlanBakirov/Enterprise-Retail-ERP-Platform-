using System.Windows;

namespace GeorgiaERP.Desktop.Views.Finance;

public partial class JournalEntryEditWindow : Window
{
    public JournalEntryEditWindow()
    {
        InitializeComponent();
    }

    private void OnCancel(object sender, RoutedEventArgs e) => Close();
}
