using System.Windows;
using System.Windows.Controls;
using GeorgiaERP.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace GeorgiaERP.Desktop.Views.Admin;

public partial class UsersView : UserControl
{
    private readonly UsersViewModel _viewModel;

    public UsersView()
    {
        InitializeComponent();
        _viewModel = App.Services.GetRequiredService<UsersViewModel>();
        DataContext = _viewModel;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        IsVisibleChanged += OnVisibilityChanged;
        if (Visibility == Visibility.Visible && _viewModel.Users.Count == 0)
            await _viewModel.LoadCommand.ExecuteAsync(null);
    }

    private async void OnVisibilityChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is Visibility v && v == Visibility.Visible && _viewModel.Users.Count == 0)
            await _viewModel.LoadCommand.ExecuteAsync(null);
    }

    private async void OnAddUser(object sender, RoutedEventArgs e)
    {
        var vm = App.Services.GetRequiredService<UserCreateViewModel>();
        var window = new UserCreateWindow { DataContext = vm, Owner = Window.GetWindow(this) };
        window.ShowDialog();
        if (vm.Saved)
            await _viewModel.LoadCommand.ExecuteAsync(null);
    }
}
