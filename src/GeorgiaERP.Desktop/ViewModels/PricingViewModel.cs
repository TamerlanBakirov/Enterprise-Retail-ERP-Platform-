using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeorgiaERP.Desktop.Models;
using GeorgiaERP.Desktop.Services;

namespace GeorgiaERP.Desktop.ViewModels;

public partial class PricingViewModel : TabbedPagedViewModel
{
    private readonly IPricingService _pricingService;

    [ObservableProperty] private PriceListDto? _selectedPriceList;

    public ObservableCollection<PriceListDto> PriceLists { get; } = [];
    public ObservableCollection<PriceListItemDto> PriceListItems { get; } = [];
    public ObservableCollection<PromotionDto> Promotions { get; } = [];

    public PricingViewModel(IPricingService pricingService)
    {
        _pricingService = pricingService;
        ActiveTab = "PriceLists";
    }

    protected override async Task LoadCoreAsync()
    {
        switch (ActiveTab)
        {
            case "PriceLists":
                var lists = await _pricingService.GetPriceListsAsync(page: CurrentPage);
                ReplaceItems(PriceLists, lists.Items);
                TotalPages = lists.TotalPages;
                TotalCount = lists.TotalCount;
                break;
            case "Promotions":
                var promos = await _pricingService.GetPromotionsAsync(page: CurrentPage);
                ReplaceItems(Promotions, promos.Items);
                TotalPages = promos.TotalPages;
                TotalCount = promos.TotalCount;
                break;
        }
    }

    [RelayCommand]
    private async Task LoadItemsAsync()
    {
        if (SelectedPriceList is null) return;
        await ExecuteAsync(async () =>
        {
            var items = await _pricingService.GetPriceListItemsAsync(SelectedPriceList.Id);
            ReplaceItems(PriceListItems, items.Items);
        });
    }

    partial void OnSelectedPriceListChanged(PriceListDto? value)
    {
        if (value is not null)
            _ = LoadItemsAsync();
        else
            PriceListItems.Clear();
    }
}
