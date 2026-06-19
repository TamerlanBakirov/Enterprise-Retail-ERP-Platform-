using System.Windows;
using GeorgiaERP.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace GeorgiaERP.Desktop.Views.Login;

public partial class LicenseActivationWindow : Window
{
    public LicenseActivationWindow()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<LicenseActivationViewModel>();
    }
}
