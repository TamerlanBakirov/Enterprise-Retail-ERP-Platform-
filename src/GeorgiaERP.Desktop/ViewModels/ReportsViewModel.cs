using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeorgiaERP.Desktop.Models;
using GeorgiaERP.Desktop.Services;

namespace GeorgiaERP.Desktop.ViewModels;

public partial class ReportsViewModel : BaseViewModel
{
    private readonly IReportService _reportService;

    [ObservableProperty] private string _activeReport = "Sales";
    [ObservableProperty] private DateTime _dateFrom = DateTime.Today.AddDays(-30);
    [ObservableProperty] private DateTime _dateTo = DateTime.Today;

    [ObservableProperty] private SalesReportDto? _salesReport;
    [ObservableProperty] private StockReportDto? _stockReport;
    [ObservableProperty] private VatReportDto? _vatReport;

    public ReportsViewModel(IReportService reportService)
    {
        _reportService = reportService;
    }

    [RelayCommand]
    private Task LoadReportAsync() => ExecuteAsync(async () =>
    {
        switch (ActiveReport)
        {
            case "Sales":
                SalesReport = await _reportService.GetSalesReportAsync(null, DateFrom, DateTo);
                break;
            case "Stock":
                StockReport = await _reportService.GetStockReportAsync();
                break;
            case "VAT":
                VatReport = await _reportService.GetVatReportAsync(DateTo.Year, DateTo.Month);
                break;
        }
    });

    [RelayCommand]
    private void Export()
    {
        switch (ActiveReport)
        {
            case "Sales" when SalesReport is not null:
                CsvExportService.ExportSalesReport(SalesReport);
                break;
            case "Stock" when StockReport is not null:
                CsvExportService.ExportStockReport(StockReport);
                break;
            case "VAT" when VatReport is not null:
                CsvExportService.ExportVatReport(VatReport);
                break;
        }
    }

    [RelayCommand]
    private async Task SwitchReportAsync(string report)
    {
        ActiveReport = report;
        await LoadReportAsync();
    }
}
