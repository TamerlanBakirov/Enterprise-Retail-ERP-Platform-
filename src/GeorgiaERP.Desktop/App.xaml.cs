using System.Windows;
using GeorgiaERP.Desktop.Services;
using GeorgiaERP.Desktop.ViewModels;
using GeorgiaERP.Desktop.Views.Login;
using Microsoft.Extensions.DependencyInjection;

namespace GeorgiaERP.Desktop;

public partial class App : Application
{
    public static ServiceProvider Services { get; private set; } = null!;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        try
        {
            var licenseService = Services.GetRequiredService<ILicenseService>();
            var licenseInfo = await licenseService.GetStatusAsync();

            if (licenseInfo is null || !licenseInfo.IsValid)
            {
                var activationWindow = new LicenseActivationWindow();
                activationWindow.Show();
                MainWindow = activationWindow;
            }
            else
            {
                var loginWindow = new LoginWindow();
                loginWindow.Show();
                MainWindow = loginWindow;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to start: {ex.Message}\n\nPlease check that the API server is running.",
                "Georgia ERP", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<INavigationService, NavigationService>();

        services.AddHttpClient("api", (sp, client) =>
        {
            var settings = sp.GetRequiredService<ISettingsService>();
            client.BaseAddress = new Uri(settings.ApiBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddSingleton<AuthTokenHandler>();
        services.AddHttpClient("api-auth", (sp, client) =>
        {
            var settings = sp.GetRequiredService<ISettingsService>();
            client.BaseAddress = new Uri(settings.ApiBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        }).AddHttpMessageHandler<AuthTokenHandler>();

        services.AddSingleton<IApiClient, ApiClient>();
        services.AddSingleton<ILicenseService, LicenseService>();
        services.AddSingleton<IUpdateService, UpdateService>();
        services.AddSingleton<IProductService, ProductService>();
        services.AddSingleton<IInventoryService, InventoryService>();
        services.AddSingleton<IPosService, PosService>();
        services.AddSingleton<ICustomerService, CustomerService>();
        services.AddSingleton<IProcurementService, ProcurementService>();
        services.AddSingleton<IFinanceService, FinanceService>();
        services.AddSingleton<IReportService, ReportService>();
        services.AddSingleton<IOrganizationService, OrganizationService>();

        services.AddTransient<LoginViewModel>();
        services.AddTransient<LicenseActivationViewModel>();
        services.AddTransient<MainViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<PosViewModel>();
        services.AddTransient<ProductsViewModel>();
        services.AddTransient<InventoryViewModel>();
        services.AddTransient<CustomersViewModel>();
        services.AddTransient<ProcurementViewModel>();
        services.AddTransient<FinanceViewModel>();
        services.AddTransient<ReportsViewModel>();
        services.AddTransient<SettingsViewModel>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Services.Dispose();
        base.OnExit(e);
    }
}
