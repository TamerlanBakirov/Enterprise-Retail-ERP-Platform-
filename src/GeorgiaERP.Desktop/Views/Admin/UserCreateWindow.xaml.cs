using System.Windows;
using GeorgiaERP.Desktop.ViewModels;

namespace GeorgiaERP.Desktop.Views.Admin;

public partial class UserCreateWindow : Window
{
    public UserCreateWindow()
    {
        InitializeComponent();
    }

    private void OnCancel(object sender, RoutedEventArgs e) => Close();

    private async void OnCreate(object sender, RoutedEventArgs e)
    {
        if (DataContext is UserCreateViewModel vm)
        {
            vm.Password = PasswordBox.Password;
            await vm.SaveCommand.ExecuteAsync(null);
        }
    }
}
