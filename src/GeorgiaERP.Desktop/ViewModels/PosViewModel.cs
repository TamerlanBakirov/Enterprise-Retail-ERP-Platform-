using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeorgiaERP.Desktop.Models;
using GeorgiaERP.Desktop.Services;

namespace GeorgiaERP.Desktop.ViewModels;

public partial class PosViewModel : ObservableObject
{
    private readonly IPosService _posService;
    private readonly IProductService _productService;

    [ObservableProperty] private PosSessionDto? _currentSession;
    [ObservableProperty] private string _barcodeInput = string.Empty;
    [ObservableProperty] private decimal _cashReceived;
    [ObservableProperty] private string _selectedPaymentMethod = "Cash";
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private string? _successMessage;

    public ObservableCollection<PosCartItem> CartItems { get; } = [];

    public decimal SubTotal => CartItems.Sum(x => x.LineTotal);
    public decimal TotalVat => CartItems.Sum(x => x.VatAmount);
    public decimal TotalDiscount => CartItems.Sum(x => x.DiscountAmount);
    public decimal GrandTotal => SubTotal;
    public decimal ChangeAmount => CashReceived > GrandTotal ? CashReceived - GrandTotal : 0;

    public string[] PaymentMethods { get; } = ["Cash", "Card", "Transfer"];

    public PosViewModel(IPosService posService, IProductService productService)
    {
        _posService = posService;
        _productService = productService;
    }

    [RelayCommand]
    private async Task ScanBarcodeAsync()
    {
        if (string.IsNullOrWhiteSpace(BarcodeInput)) return;

        ErrorMessage = null;
        try
        {
            var products = await _productService.GetProductsAsync(search: BarcodeInput, pageSize: 1);
            if (products.Items.Count == 0)
            {
                ErrorMessage = $"Product not found: {BarcodeInput}";
                return;
            }

            var product = products.Items[0];
            var existing = CartItems.FirstOrDefault(x => x.ProductId == product.Id);
            if (existing is not null)
            {
                existing.Quantity++;
                existing.Recalculate();
            }
            else
            {
                CartItems.Add(new PosCartItem
                {
                    ProductId = product.Id,
                    Barcode = product.Barcode,
                    ProductName = product.Name,
                    UnitPrice = product.RetailPrice,
                    VatRate = product.VatRate,
                    Quantity = 1
                });
            }
            OnPropertyChanged(nameof(SubTotal));
            OnPropertyChanged(nameof(TotalVat));
            OnPropertyChanged(nameof(GrandTotal));
            OnPropertyChanged(nameof(ChangeAmount));
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            BarcodeInput = string.Empty;
        }
    }

    [RelayCommand]
    private void RemoveItem(PosCartItem item)
    {
        CartItems.Remove(item);
        OnPropertyChanged(nameof(SubTotal));
        OnPropertyChanged(nameof(TotalVat));
        OnPropertyChanged(nameof(GrandTotal));
        OnPropertyChanged(nameof(ChangeAmount));
    }

    [RelayCommand]
    private async Task CompleteSaleAsync()
    {
        if (CurrentSession is null)
        {
            ErrorMessage = "No active POS session. Open a session first.";
            return;
        }

        if (CartItems.Count == 0)
        {
            ErrorMessage = "Cart is empty";
            return;
        }

        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var request = new CreatePosTransactionRequest(
                CurrentSession.Id,
                null,
                CartItems.Select(c => new PosLineRequest(c.ProductId, c.Barcode, c.Quantity, c.DiscountAmount > 0 ? c.DiscountAmount : null)).ToList(),
                [new PosPaymentRequest(SelectedPaymentMethod, SelectedPaymentMethod == "Cash" ? CashReceived : GrandTotal)]
            );

            var result = await _posService.CreateTransactionAsync(request);
            if (result is not null)
            {
                SuccessMessage = $"Sale completed: {result.TransactionNumber} — {result.GrandTotal:N2} GEL";
                CartItems.Clear();
                CashReceived = 0;
                OnPropertyChanged(nameof(SubTotal));
                OnPropertyChanged(nameof(TotalVat));
                OnPropertyChanged(nameof(GrandTotal));
                OnPropertyChanged(nameof(ChangeAmount));
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void ClearCart()
    {
        CartItems.Clear();
        CashReceived = 0;
        ErrorMessage = null;
        SuccessMessage = null;
        OnPropertyChanged(nameof(SubTotal));
        OnPropertyChanged(nameof(TotalVat));
        OnPropertyChanged(nameof(GrandTotal));
        OnPropertyChanged(nameof(ChangeAmount));
    }

    partial void OnCashReceivedChanged(decimal value) => OnPropertyChanged(nameof(ChangeAmount));
}

public partial class PosCartItem : ObservableObject
{
    public Guid ProductId { get; set; }
    public string? Barcode { get; set; }

    [ObservableProperty] private string _productName = string.Empty;
    [ObservableProperty] private decimal _quantity = 1;
    [ObservableProperty] private decimal _unitPrice;
    [ObservableProperty] private decimal _discountAmount;
    [ObservableProperty] private decimal _vatAmount;
    [ObservableProperty] private decimal _lineTotal;

    public decimal VatRate { get; set; } = 0.18m;

    public void Recalculate()
    {
        var gross = UnitPrice * Quantity;
        LineTotal = gross - DiscountAmount;
        VatAmount = LineTotal * VatRate / (1 + VatRate);
    }

    partial void OnQuantityChanged(decimal value) => Recalculate();
    partial void OnDiscountAmountChanged(decimal value) => Recalculate();
}
