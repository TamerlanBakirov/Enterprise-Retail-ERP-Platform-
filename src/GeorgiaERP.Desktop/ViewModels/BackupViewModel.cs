using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeorgiaERP.Desktop.Models;
using GeorgiaERP.Desktop.Services;

namespace GeorgiaERP.Desktop.ViewModels;

public partial class BackupViewModel : BaseViewModel
{
    private readonly IBackupService _backupService;

    [ObservableProperty] private ObservableCollection<BackupRecordDto> _backups = new();
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _totalPages = 1;
    [ObservableProperty] private string _selectedBackupType = "Full";
    [ObservableProperty] private string? _backupNotes;
    [ObservableProperty] private bool _isCreating;

    public string[] BackupTypes { get; } = ["Full", "SchemaOnly", "DataOnly"];

    public BackupViewModel(IBackupService backupService)
    {
        _backupService = backupService;
    }

    [RelayCommand]
    private Task LoadBackupsAsync() => ExecuteAsync(async () =>
    {
        var result = await _backupService.ListBackupsAsync(CurrentPage);
        if (result is not null)
        {
            Backups = new ObservableCollection<BackupRecordDto>(result.Items);
            TotalPages = result.TotalPages > 0 ? result.TotalPages : 1;
        }
    });

    [RelayCommand]
    private Task CreateBackupAsync() => ExecuteAsync(async () =>
    {
        IsCreating = true;
        try
        {
            await _backupService.CreateBackupAsync(SelectedBackupType, BackupNotes);
            BackupNotes = null;
            await LoadBackupsAsync();
        }
        finally
        {
            IsCreating = false;
        }
    });

    [RelayCommand]
    private Task DeleteBackupAsync(BackupRecordDto backup) => ExecuteAsync(async () =>
    {
        await _backupService.DeleteBackupAsync(backup.Id);
        await LoadBackupsAsync();
    });

    [RelayCommand]
    private Task NextPageAsync() => ExecuteAsync(async () =>
    {
        if (CurrentPage < TotalPages)
        {
            CurrentPage++;
            await LoadBackupsAsync();
        }
    });

    [RelayCommand]
    private Task PreviousPageAsync() => ExecuteAsync(async () =>
    {
        if (CurrentPage > 1)
        {
            CurrentPage--;
            await LoadBackupsAsync();
        }
    });
}
