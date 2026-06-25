using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeorgiaERP.Desktop.Models;
using GeorgiaERP.Desktop.Services;

namespace GeorgiaERP.Desktop.ViewModels;

public partial class PosViewModel : BaseViewModel
{
    private readonly IPosService _posService;
    private readonly IProductService _productService;

    [ObservableProperty] private PosSessionDto? _currentSession;
    [ObservableProperty] private string _barcodeInput = string.Empty;
    [ObservableProperty] private decimal _cashReceived;
    [ObservableProperty] private string _selectedPaymentMethod = "Cash";
    [ObservableProperty] private string? _successMessage;

    [ObservableProperty] private TerminalDto? _selectedTerminal;
    [ObservableProperty] private decimal _openingBalance;

    public ObservableCollection<TerminalDto> Terminals { get; } = [];
    public ObservableCollection<PosCartItem> CartItems { get; } = [];

    /// <summary>True when a POS session is open and sales can be made.</summary>
    public bool HasSession => CurrentSession is not null;

    /// <summary>True when no session is open (terminal picker is shown).</summary>
    public bool NoSession => CurrentSession is null;

    /// <summary>Header text describing the current session state.</summary>
    public string SessionInfo => CurrentSession is null
        ? "No open session"
        : $"Session open — {CurrentSession.TerminalName} · cashier {CurrentSession.CashierName} · opened {CurrentSession.OpenedAt.LocalDateTime:g}";

    partial void OnCurrentSessionChanged(PosSessionDto? value)
    {
        OnPropertyChanged(nameof(HasSession));
        OnPropertyChanged(nameof(NoSession));
        OnPropertyChanged(nameof(SessionInfo));
    }

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

    /// <summary>
    /// Loads any already-open session for this user; if none exists, loads the
    /// terminal list so the cashier can open a new session.
    /// </summary>
    public async Task InitializeAsync()
    {
        await ExecuteAsync(async () =>
        {
            var open = await _posService.GetSessionsAsync(status: "Open", pageSize: 1);
            if (open.Items.Count > 0)
            {
                CurrentSession = open.Items[0];
                return;
            }

            var terminals = await _posService.GetTerminalsAsync(isActive: true);
            ReplaceItems(Terminals, terminals);
            SelectedTerminal ??= Terminals.FirstOrDefault();
        });
    }

    [RelayCommand]
    private async Task OpenSessionAsync()
    {
        if (SelectedTerminal is null)
        {
            ErrorMessage = "Select a terminal to open a session.";
            return;
        }

        await ExecuteAsync(async () =>
        {
            var session = await _posService.OpenSessionAsync(
                new OpenPosSessionRequest(SelectedTerminal.Id, OpeningBalance));
            if (session is not null)
            {
                CurrentSession = session;
                SuccessMessage = $"Session opened on {session.TerminalName}.";
            }
            else
            {
                ErrorMessage = "Failed to open session.";
            }
        });
    }

    [RelayCommand]
    private async Task CloseSessionAsync()
    {
        if (CurrentSession is null) return;

        await ExecuteAsync(async () =>
        {
            var result = await _posService.CloseSessionAsync(
                CurrentSession.Id, new ClosePosSessionRequest(CashReceived));
            if (result.IsSuccess)
            {
                CurrentSession = null;
                CartItems.Clear();
                NotifyCartTotals();
                SuccessMessage = "Session closed.";
                await InitializeAsync();
            }
            else
            {
                ErrorMessage = result.Error ?? "Failed to close session.";
            }
        });
    }

    private void NotifyCartTotals()
    {
        OnPropertyChanged(nameof(SubTotal));
        OnPropertyChanged(nameof(TotalVat));
        OnPropertyChanged(nameof(GrandTotal));
        OnPropertyChanged(nameof(ChangeAmount));
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
                var item = new PosCartItem
                {
                    ProductId = product.Id,
                    Barcode = product.Barcode,
                    ProductName = product.Name,
                    UnitPrice = product.RetailPrice,
                    VatRate = product.VatRate,
                    Quantity = 1
                };
                item.Recalculate();
                item.PropertyChanged += (_, _) => NotifyCartTotals();
                CartItems.Add(item);
            }
            NotifyCartTotals();
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
        NotifyCartTotals();
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

        await ExecuteAsync(async () =>
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
                NotifyCartTotals();
            }
        });
    }

    [RelayCommand]
    private void ClearCart()
    {
        CartItems.Clear();
        CashReceived = 0;
        ErrorMessage = null;
        SuccessMessage = null;
        NotifyCartTotals();
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
