using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeorgiaERP.Desktop.Models;
using GeorgiaERP.Desktop.Services;

namespace GeorgiaERP.Desktop.ViewModels;

public partial class WaybillEditViewModel : DialogViewModel
{
    private readonly IComplianceService _complianceService;

    [ObservableProperty] private string _waybillType = "Inner";
    [ObservableProperty] private string _buyerTin = string.Empty;
    [ObservableProperty] private string? _buyerName;
    [ObservableProperty] private string _sellerTin = string.Empty;
    [ObservableProperty] private string? _sellerName;
    [ObservableProperty] private string? _startAddress;
    [ObservableProperty] private string? _endAddress;
    [ObservableProperty] private string? _vehicleNumber;
    [ObservableProperty] private string? _driverTin;
    [ObservableProperty] private string? _transportType;

    public ObservableCollection<WaybillGoodsLineViewModel> GoodsLines { get; } = [];

    public WaybillEditViewModel(IComplianceService complianceService)
    {
        _complianceService = complianceService;
        GoodsLines.Add(new WaybillGoodsLineViewModel());
    }

    [RelayCommand]
    private void AddLine() => GoodsLines.Add(new WaybillGoodsLineViewModel());

    [RelayCommand]
    private void RemoveLine(WaybillGoodsLineViewModel line)
    {
        if (GoodsLines.Count > 1) GoodsLines.Remove(line);
    }

    [RelayCommand]
    private async Task LookupBuyerAsync()
    {
        if (string.IsNullOrWhiteSpace(BuyerTin)) return;
        try
        {
            var result = await _complianceService.LookupTinNameAsync(BuyerTin);
            if (result?.Name is not null) BuyerName = result.Name;
        }
        catch { }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(BuyerTin) || string.IsNullOrWhiteSpace(SellerTin))
        {
            ErrorMessage = "Buyer TIN and Seller TIN are required.";
            return;
        }

        ErrorMessage = null;
        try
        {
            var goods = GoodsLines
                .Where(l => !string.IsNullOrWhiteSpace(l.ProductName))
                .Select(l => new WaybillGoodsItem(l.ProductName!, l.UnitId, l.Quantity, l.Price, l.BarCode))
                .ToList();

            var request = new CreateWaybillRequest(
                WaybillType, BuyerTin, BuyerName, SellerTin, SellerName,
                StartAddress, EndAddress, VehicleNumber, DriverTin, TransportType,
                null, null, null, goods);

            var result = await _complianceService.CreateWaybillAsync(request);
            if (result is not null) SaveAndClose();
            else ErrorMessage = "Failed to create waybill.";
        }
        catch (Exception ex) { ErrorMessage = ex.Message; }
    }
}

public partial class WaybillGoodsLineViewModel : ObservableObject
{
    [ObservableProperty] private string? _productName;
    [ObservableProperty] private int _unitId = 99;
    [ObservableProperty] private decimal _quantity = 1;
    [ObservableProperty] private decimal _price;
    [ObservableProperty] private string? _barCode;
}
