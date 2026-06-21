using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeorgiaERP.Desktop.Models;
using GeorgiaERP.Desktop.Services;

namespace GeorgiaERP.Desktop.ViewModels;

public partial class PromotionEditViewModel : DialogViewModel
{
    private readonly IPricingService _pricingService;

    [ObservableProperty] private string _code = string.Empty;
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string? _nameKa;
    [ObservableProperty] private string _promotionType = "Percentage";
    [ObservableProperty] private decimal? _discountValue;
    [ObservableProperty] private string? _conditions;
    [ObservableProperty] private DateTimeOffset _validFrom = DateTimeOffset.Now;
    [ObservableProperty] private DateTimeOffset? _validTo;
    [ObservableProperty] private int? _maxUses;

    public PromotionEditViewModel(IPricingService pricingService)
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
            var request = new CreatePromotionRequest(Code, Name, NameKa, PromotionType, DiscountValue, Conditions, ValidFrom, ValidTo, MaxUses);
            var result = await _pricingService.CreatePromotionAsync(request);
            if (result is not null) SaveAndClose();
            else ErrorMessage = "Failed to create promotion.";
        }
        catch (Exception ex) { ErrorMessage = ex.Message; }
    }
}
