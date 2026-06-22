using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeorgiaERP.Desktop.Models;
using GeorgiaERP.Desktop.Services;

namespace GeorgiaERP.Desktop.ViewModels;

public partial class SupplierEditViewModel : DialogViewModel
{
    private readonly IProcurementService _procurementService;

    [ObservableProperty] private string _code = string.Empty;
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string? _nameKa;
    [ObservableProperty] private string? _taxId;
    [ObservableProperty] private string? _phone;
    [ObservableProperty] private string? _email;
    [ObservableProperty] private string? _address;
    [ObservableProperty] private int? _paymentTermDays;
    [ObservableProperty] private bool _isVatPayer;

    public SupplierEditViewModel(IProcurementService procurementService)
    {
        _procurementService = procurementService;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Code) || string.IsNullOrWhiteSpace(Name))
        {
            ErrorMessage = "Code and Name are required.";
            return;
        }

        ErrorMessage = null;
        try
        {
            var request = new CreateSupplierRequest(Code, Name, NameKa, TaxId, Phone, Email, Address, PaymentTermDays, IsVatPayer);
            var result = await _procurementService.CreateSupplierAsync(request);
            if (result is not null)
                SaveAndClose();
            else
                ErrorMessage = "Failed to save supplier.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }
}
