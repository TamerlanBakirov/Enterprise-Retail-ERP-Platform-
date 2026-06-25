using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using GeorgiaERP.Desktop.Models;
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

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        try
        {
#if DEBUG
            var settings = Services.GetRequiredService<ISettingsService>();
            var authService = Services.GetRequiredService<IAuthService>();
            var (success, error) = await authService.LoginAsync("admin", "Admin@123!");

            if (!success)
            {
                // Auto-login failed: without a token every data call returns 401,
                // so the app would look empty. Surface the reason instead of
                // silently falling back to a tokenless fake user.
                MessageBox.Show(
                    $"Auto-login failed — the app will have NO data.\n\n" +
                    $"Reason: {error}\n\n" +
                    $"Make sure the API is running and reachable at:\n{settings.ApiBaseUrl}\n\n" +
                    $"Start it with:  dotnet run --project src/GeorgiaERP.Api",
                    "Georgia ERP — API not reachable",
                    MessageBoxButton.OK, MessageBoxImage.Warning);

                ((AuthService)authService).CurrentUser = new UserInfo(
                    Guid.Empty, "admin", "admin@dev.local",
                    "Dev", "Admin", "დევ", "ადმინი", "ka",
                    new[] { "Administrator" }, null);
            }

            var mainWindow = new Views.Shell.MainWindow();
            mainWindow.Show();
            MainWindow = mainWindow;
#else
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
#endif
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to start: {ex.Message}\n\nPlease check that the API server is running.",
                "Georgia ERP", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    private static readonly string ErrorLogPath = System.IO.Path.Combine(
        System.IO.Path.GetTempPath(), "georgiaerp-error.log");

    private static void LogError(string source, Exception ex)
    {
        try
        {
            System.IO.File.AppendAllText(ErrorLogPath,
                $"[{DateTimeOffset.Now:HH:mm:ss}] {source}: {ex}\n\n");
        }
        catch { }
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        LogError("Dispatcher", e.Exception);
        MessageBox.Show(
            $"An unexpected error occurred:\n\n{e.Exception.Message}",
            "Georgia ERP — Error", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        // Was silently swallowed — log it so background/fire-and-forget failures
        // (e.g. a view's data load) are diagnosable instead of vanishing.
        LogError("UnobservedTask", e.Exception);
        e.SetObserved();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IOfflineQueueService, OfflineQueueService>();
        services.AddSingleton<ILocalizationService, LocalizationService>();

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
        services.AddSingleton<IWarehouseService, WarehouseService>();
        services.AddSingleton<IUserService, UserService>();
        services.AddSingleton<IPricingService, PricingService>();
        services.AddSingleton<IComplianceService, ComplianceService>();
        services.AddSingleton<ISignalRNotificationService, SignalRNotificationService>();
        services.AddSingleton<IToastNotificationService, ToastNotificationService>();
        services.AddSingleton<IBackupService, BackupService>();
        services.AddSingleton<IAuditService, AuditService>();

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
        services.AddTransient<UsersViewModel>();
        services.AddTransient<UserCreateViewModel>();
        services.AddTransient<ProductEditViewModel>();
        services.AddTransient<CustomerEditViewModel>();
        services.AddTransient<WarehouseViewModel>();
        services.AddTransient<AccountEditViewModel>();
        services.AddTransient<JournalEntryEditViewModel>();
        services.AddTransient<BankAccountEditViewModel>();
        services.AddTransient<SupplierEditViewModel>();
        services.AddTransient<PurchaseOrderEditViewModel>();
        services.AddTransient<TransferOrderEditViewModel>();
        services.AddTransient<StockCountEditViewModel>();
        services.AddTransient<TwoFactorSetupViewModel>();
        services.AddTransient<ComplianceViewModel>();
        services.AddTransient<WaybillEditViewModel>();
        services.AddTransient<PricingViewModel>();
        services.AddTransient<PriceListEditViewModel>();
        services.AddTransient<PromotionEditViewModel>();
        services.AddTransient<SetPriceViewModel>();
        services.AddTransient<WarehouseEditViewModel>();
        services.AddTransient<LocationEditViewModel>();
        services.AddTransient<ReceivingOrderEditViewModel>();
        services.AddTransient<ShippingOrderEditViewModel>();
        services.AddTransient<BackupViewModel>();
        services.AddTransient<AuditViewModel>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Services.Dispose();
        base.OnExit(e);
    }
}
