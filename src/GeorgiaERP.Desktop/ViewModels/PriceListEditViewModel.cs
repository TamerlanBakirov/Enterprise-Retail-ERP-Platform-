using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeorgiaERP.Desktop.Models;
using GeorgiaERP.Desktop.Services;

namespace GeorgiaERP.Desktop.ViewModels;

public partial class PriceListEditViewModel : DialogViewModel
{
    private readonly IPricingService _pricingService;

    [ObservableProperty] private string _code = string.Empty;
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string? _nameKa;
    [ObservableProperty] private string _priceType = "Retail";
    [ObservableProperty] private DateTimeOffset _validFrom = DateTimeOffset.Now;
    [ObservableProperty] private DateTimeOffset? _validTo;
    [ObservableProperty] private int _priority = 1;

    public PriceListEditViewModel(IPricingService pricingService)
    {
        _pricingService = pricingService;
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
            var request = new CreatePriceListRequest(Code, Name, NameKa, PriceType, null, ValidFrom, ValidTo, Priority);
            var result = await _pricingService.CreatePriceListAsync(request);
            if (result is not null) SaveAndClose();
            else ErrorMessage = "Failed to create price list.";
        }
        catch (Exception ex) { ErrorMessage = ex.Message; }
    }
}
