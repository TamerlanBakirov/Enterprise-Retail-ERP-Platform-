using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeorgiaERP.Desktop.Models;
using GeorgiaERP.Desktop.Services;

namespace GeorgiaERP.Desktop.ViewModels;

public partial class ComplianceViewModel : TabbedPagedViewModel
{
    private readonly IComplianceService _complianceService;

    [ObservableProperty] private string _rsGeStatus = "Unknown";
    [ObservableProperty] private WaybillDto? _selectedWaybill;
    [ObservableProperty] private VatSummaryDto? _vatSummary;
    [ObservableProperty] private DeadlinesResponse? _deadlines;
    [ObservableProperty] private string _tinInput = string.Empty;
    [ObservableProperty] private string? _tinResult;
    [ObservableProperty] private int _vatYear;
    [ObservableProperty] private int _vatMonth;

    public ObservableCollection<WaybillDto> Waybills { get; } = [];
    public ObservableCollection<FiscalDocumentDto> FiscalDocuments { get; } = [];

    public ComplianceViewModel(IComplianceService complianceService)
    {
        _complianceService = complianceService;
        ActiveTab = "Waybills";
        var now = DateTime.Now;
        VatYear = now.Year;
        VatMonth = now.Month;
    }

    protected override async Task LoadCoreAsync()
    {
        var health = await _complianceService.CheckRsGeHealthAsync();
        RsGeStatus = health?.Status ?? "Unavailable";

        switch (ActiveTab)
        {
            case "Waybills":
                var waybills = await _complianceService.GetWaybillsAsync(page: CurrentPage);
                ReplaceItems(Waybills, waybills);
                break;
            case "FiscalDocuments":
                var docs = await _complianceService.GetFiscalDocumentsAsync(page: CurrentPage);
                ReplaceItems(FiscalDocuments, docs);
                break;
            case "VatSummary":
                VatSummary = await _complianceService.GetVatSummaryAsync(VatYear, VatMonth);
                break;
            case "Deadlines":
                Deadlines = await _complianceService.GetDeadlinesAsync();
                break;
        }
    }

    [RelayCommand]
    private async Task ConfirmWaybillAsync()
    {
        if (SelectedWaybill is null) return;
        var result = await _complianceService.ConfirmWaybillAsync(SelectedWaybill.FiscalDocumentId);
        if (result.IsSuccess) await LoadAsync();
        else ErrorMessage = result.Error;
    }

    [RelayCommand]
    private async Task CloseWaybillAsync()
    {
        if (SelectedWaybill is null) return;
        var result = await _complianceService.CloseWaybillAsync(SelectedWaybill.FiscalDocumentId);
        if (result.IsSuccess) await LoadAsync();
        else ErrorMessage = result.Error;
    }

    [RelayCommand]
    private async Task LookupTinAsync()
    {
        if (string.IsNullOrWhiteSpace(TinInput)) return;
        await ExecuteAsync(async () =>
        {
            var nameResult = await _complianceService.LookupTinNameAsync(TinInput);
            var vatResult = await _complianceService.GetVatStatusAsync(TinInput);
            TinResult = $"Name: {nameResult?.Name ?? "Not found"}\nVAT Payer: {vatResult?.IsVatPayer}";
        });
    }
}
