using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeorgiaERP.Desktop.Models;
using GeorgiaERP.Desktop.Services;

namespace GeorgiaERP.Desktop.ViewModels;

public partial class AuditViewModel : BaseViewModel
{
    private readonly IAuditService _auditService;

    [ObservableProperty] private ObservableCollection<AuditLogDto> _auditLogs = new();
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _totalPages = 1;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private string? _entityTypeFilter;
    [ObservableProperty] private string? _entityIdFilter;
    [ObservableProperty] private DateTime _dateFrom = DateTime.Today.AddDays(-7);
    [ObservableProperty] private DateTime _dateTo = DateTime.Today;

    public AuditViewModel(IAuditService auditService)
    {
        _auditService = auditService;
    }

    [RelayCommand]
    private Task LoadAuditLogsAsync() => ExecuteAsync(async () =>
    {
        var result = await _auditService.GetAuditLogsAsync(
            EntityTypeFilter, EntityIdFilter, null,
            DateFrom, DateTo.AddDays(1),
            CurrentPage);

        AuditLogs = new ObservableCollection<AuditLogDto>(result.Items);
        TotalCount = result.TotalCount;
        TotalPages = result.TotalPages > 0 ? result.TotalPages : 1;
    });

    [RelayCommand]
    private Task SearchAsync() => ExecuteAsync(async () =>
    {
        CurrentPage = 1;
        await LoadAuditLogsAsync();
    });

    [RelayCommand]
    private void ClearFilters()
    {
        EntityTypeFilter = null;
        EntityIdFilter = null;
        DateFrom = DateTime.Today.AddDays(-7);
        DateTo = DateTime.Today;
    }

    [RelayCommand]
    private Task NextPageAsync() => ExecuteAsync(async () =>
    {
        if (CurrentPage < TotalPages)
        {
            CurrentPage++;
            await LoadAuditLogsAsync();
        }
    });

    [RelayCommand]
    private Task PreviousPageAsync() => ExecuteAsync(async () =>
    {
        if (CurrentPage > 1)
        {
            CurrentPage--;
            await LoadAuditLogsAsync();
        }
    });
}
