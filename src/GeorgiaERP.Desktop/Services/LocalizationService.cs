namespace GeorgiaERP.Desktop.Services;

public interface ILocalizationService
{
    string CurrentLanguage { get; }
    string Get(string key);
    void SetLanguage(string language);
    event Action? LanguageChanged;
}

public class LocalizationService : ILocalizationService
{
    private readonly ISettingsService _settings;
    private Dictionary<string, string> _strings = null!;

    public string CurrentLanguage { get; private set; }
    public event Action? LanguageChanged;

    public LocalizationService(ISettingsService settings)
    {
        _settings = settings;
        CurrentLanguage = settings.Language;
        LoadStrings(CurrentLanguage);
    }

    public string Get(string key) =>
        _strings.TryGetValue(key, out var value) ? value : key;

    public void SetLanguage(string language)
    {
        CurrentLanguage = language;
        _settings.Language = language;
        _settings.Save();
        LoadStrings(language);
        LanguageChanged?.Invoke();
    }

    private void LoadStrings(string lang)
    {
        _strings = lang == "ka" ? Georgian() : English();
    }

    private static Dictionary<string, string> English() => new()
    {
        ["app.title"] = "Georgia ERP",
        ["nav.dashboard"] = "Dashboard",
        ["nav.pos"] = "POS / Sales",
        ["nav.products"] = "Products",
        ["nav.inventory"] = "Inventory",
        ["nav.customers"] = "Customers",
        ["nav.procurement"] = "Procurement",
        ["nav.finance"] = "Finance",
        ["nav.reports"] = "Reports",
        ["nav.settings"] = "Settings",
        ["nav.logout"] = "Logout",
        ["dashboard.welcome"] = "Welcome",
        ["dashboard.today_revenue"] = "Today's Revenue",
        ["dashboard.transactions"] = "Transactions",
        ["dashboard.stock_value"] = "Stock Value",
        ["dashboard.low_stock"] = "Low Stock Alerts",
        ["dashboard.total_products"] = "Total Products",
        ["dashboard.quick_actions"] = "Quick Actions",
        ["dashboard.new_sale"] = "New Sale",
        ["dashboard.add_product"] = "Add Product",
        ["dashboard.stock_count"] = "Stock Count",
        ["dashboard.reports"] = "Reports",
        ["pos.title"] = "Point of Sale",
        ["pos.payment"] = "Payment",
        ["pos.subtotal"] = "Subtotal",
        ["pos.vat"] = "VAT (18%)",
        ["pos.discount"] = "Discount",
        ["pos.total"] = "TOTAL",
        ["pos.payment_method"] = "Payment Method",
        ["pos.cash_received"] = "Cash Received",
        ["pos.change"] = "Change",
        ["pos.complete_sale"] = "Complete Sale",
        ["pos.clear_cart"] = "Clear Cart",
        ["products.title"] = "Products",
        ["products.search"] = "Search",
        ["inventory.title"] = "Inventory",
        ["inventory.stock_levels"] = "Stock Levels",
        ["inventory.movements"] = "Movements",
        ["inventory.transfers"] = "Transfers",
        ["customers.title"] = "Customers",
        ["procurement.title"] = "Procurement",
        ["procurement.suppliers"] = "Suppliers",
        ["procurement.purchase_orders"] = "Purchase Orders",
        ["finance.title"] = "Finance",
        ["finance.accounts"] = "Chart of Accounts",
        ["finance.journal"] = "Journal Entries",
        ["finance.bank"] = "Bank Accounts",
        ["reports.title"] = "Reports",
        ["reports.sales"] = "Sales",
        ["reports.stock"] = "Stock",
        ["reports.vat"] = "VAT",
        ["reports.generate"] = "Generate",
        ["settings.title"] = "Settings",
        ["settings.server_url"] = "Server URL",
        ["settings.language"] = "Language",
        ["settings.save"] = "Save Settings",
        ["settings.saved"] = "Settings saved",
        ["settings.license"] = "License",
        ["settings.license_valid"] = "Active",
        ["settings.license_company"] = "Company",
        ["settings.license_expires"] = "Expires",
        ["settings.check_updates"] = "Check for Updates",
        ["settings.no_updates"] = "You are running the latest version.",
        ["settings.update_available"] = "Update available",
        ["settings.version"] = "Version",
        ["settings.about"] = "About",
        ["common.refresh"] = "Refresh",
        ["common.save"] = "Save",
        ["common.cancel"] = "Cancel",
        ["common.add"] = "Add",
        ["common.edit"] = "Edit",
        ["common.delete"] = "Delete",
        ["common.page"] = "Page",
        ["common.of"] = "of",
        ["common.items"] = "items",
        ["common.previous"] = "Previous",
        ["common.next"] = "Next",
        ["common.loading"] = "Loading...",
        ["common.error"] = "Error",
        ["common.from"] = "From:",
        ["common.to"] = "To:",
        ["update.banner"] = "A new version ({0}) is available.",
        ["update.download"] = "Download",
        ["update.dismiss"] = "Dismiss",
        ["offline.pending"] = "{0} pending operation(s)",
        ["offline.retry"] = "Retry"
    };

    private static Dictionary<string, string> Georgian() => new()
    {
        ["app.title"] = "Georgia ERP",
        ["nav.dashboard"] = "სამუშაო პანელი",
        ["nav.pos"] = "გაყიდვა",
        ["nav.products"] = "პროდუქტები",
        ["nav.inventory"] = "ინვენტარი",
        ["nav.customers"] = "მომხმარებლები",
        ["nav.procurement"] = "შესყიდვები",
        ["nav.finance"] = "ფინანსები",
        ["nav.reports"] = "ანგარიშები",
        ["nav.settings"] = "პარამეტრები",
        ["nav.logout"] = "გამოსვლა",
        ["dashboard.welcome"] = "მოგესალმებით",
        ["dashboard.today_revenue"] = "დღეს შემოსავალი",
        ["dashboard.transactions"] = "ტრანზაქციები",
        ["dashboard.stock_value"] = "მარაგის ღირებულება",
        ["dashboard.low_stock"] = "დაბალი მარაგი",
        ["dashboard.total_products"] = "სულ პროდუქტები",
        ["dashboard.quick_actions"] = "სწრაფი მოქმედებები",
        ["dashboard.new_sale"] = "ახალი გაყიდვა",
        ["dashboard.add_product"] = "პროდუქტის დამატება",
        ["dashboard.stock_count"] = "ინვენტარიზაცია",
        ["dashboard.reports"] = "ანგარიშები",
        ["pos.title"] = "გაყიდვის წერტილი",
        ["pos.payment"] = "გადახდა",
        ["pos.subtotal"] = "ჯამი",
        ["pos.vat"] = "დღგ (18%)",
        ["pos.discount"] = "ფასდაკლება",
        ["pos.total"] = "სულ ჯამი",
        ["pos.payment_method"] = "გადახდის მეთოდი",
        ["pos.cash_received"] = "მიღებული თანხა",
        ["pos.change"] = "ხურდა",
        ["pos.complete_sale"] = "გაყიდვის დასრულება",
        ["pos.clear_cart"] = "კალათის გასუფთავება",
        ["products.title"] = "პროდუქტები",
        ["products.search"] = "ძებნა",
        ["inventory.title"] = "ინვენტარი",
        ["inventory.stock_levels"] = "მარაგის დონეები",
        ["inventory.movements"] = "მოძრაობები",
        ["inventory.transfers"] = "გადაცემები",
        ["customers.title"] = "მომხმარებლები",
        ["procurement.title"] = "შესყიდვები",
        ["procurement.suppliers"] = "მომწოდებლები",
        ["procurement.purchase_orders"] = "შესყიდვის ორდერები",
        ["finance.title"] = "ფინანსები",
        ["finance.accounts"] = "ანგარიშების გეგმა",
        ["finance.journal"] = "საბუღალტრო ჩანაწერები",
        ["finance.bank"] = "საბანკო ანგარიშები",
        ["reports.title"] = "ანგარიშები",
        ["reports.sales"] = "გაყიდვები",
        ["reports.stock"] = "მარაგი",
        ["reports.vat"] = "დღგ",
        ["reports.generate"] = "გენერირება",
        ["settings.title"] = "პარამეტრები",
        ["settings.server_url"] = "სერვერის URL",
        ["settings.language"] = "ენა",
        ["settings.save"] = "შენახვა",
        ["settings.saved"] = "პარამეტრები შენახულია",
        ["settings.license"] = "ლიცენზია",
        ["settings.license_valid"] = "აქტიური",
        ["settings.license_company"] = "კომპანია",
        ["settings.license_expires"] = "ვადა",
        ["settings.check_updates"] = "განახლებების შემოწმება",
        ["settings.no_updates"] = "თქვენ უკვე უახლესი ვერსია გაქვთ.",
        ["settings.update_available"] = "განახლება ხელმისაწვდომია",
        ["settings.version"] = "ვერსია",
        ["settings.about"] = "შესახებ",
        ["common.refresh"] = "განახლება",
        ["common.save"] = "შენახვა",
        ["common.cancel"] = "გაუქმება",
        ["common.add"] = "დამატება",
        ["common.edit"] = "რედაქტირება",
        ["common.delete"] = "წაშლა",
        ["common.page"] = "გვერდი",
        ["common.of"] = "-დან",
        ["common.items"] = "ელემენტი",
        ["common.previous"] = "წინა",
        ["common.next"] = "შემდეგი",
        ["common.loading"] = "იტვირთება...",
        ["common.error"] = "შეცდომა",
        ["common.from"] = "დან:",
        ["common.to"] = "მდე:",
        ["update.banner"] = "ახალი ვერსია ({0}) ხელმისაწვდომია.",
        ["update.download"] = "ჩამოტვირთვა",
        ["update.dismiss"] = "დახურვა",
        ["offline.pending"] = "{0} მოლოდინე ოპერაცია",
        ["offline.retry"] = "თავიდან ცდა"
    };
}
