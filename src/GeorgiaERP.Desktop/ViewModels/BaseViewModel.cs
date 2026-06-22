using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace GeorgiaERP.Desktop.ViewModels;

/// <summary>
/// Base class for all ViewModels providing common loading state, error handling,
/// and pagination functionality to eliminate duplication across module ViewModels.
/// </summary>
public abstract partial class BaseViewModel : ObservableObject
{
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _errorMessage;

    /// <summary>
    /// Executes an async operation with standardized loading state and error handling.
    /// Sets IsLoading = true, clears ErrorMessage, runs the action, and handles exceptions.
    /// </summary>
    protected async Task ExecuteAsync(Func<Task> action)
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            await action();
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

    /// <summary>
    /// Replaces all items in an ObservableCollection with items from a source enumerable.
    /// Avoids repeated Clear/foreach patterns across ViewModels.
    /// </summary>
    protected static void ReplaceItems<T>(ObservableCollection<T> collection, IEnumerable<T> items)
    {
        collection.Clear();
        foreach (var item in items)
            collection.Add(item);
    }
}

/// <summary>
/// Base class for list ViewModels with built-in search and pagination support.
/// Provides CurrentPage, TotalPages, TotalCount, and Search/NextPage/PreviousPage commands.
/// </summary>
public abstract partial class PagedViewModel : BaseViewModel
{
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _totalPages = 1;
    [ObservableProperty] private int _totalCount;

    /// <summary>
    /// Override to provide the data-loading logic. Called by Load, Search, NextPage, and PreviousPage.
    /// </summary>
    protected abstract Task LoadCoreAsync();

    [RelayCommand]
    protected Task LoadAsync() => ExecuteAsync(LoadCoreAsync);

    [RelayCommand]
    protected async Task SearchAsync()
    {
        CurrentPage = 1;
        await LoadAsync();
    }

    [RelayCommand]
    protected async Task NextPageAsync()
    {
        if (CurrentPage < TotalPages)
        {
            CurrentPage++;
            await LoadAsync();
        }
    }

    [RelayCommand]
    protected async Task PreviousPageAsync()
    {
        if (CurrentPage > 1)
        {
            CurrentPage--;
            await LoadAsync();
        }
    }

    /// <summary>
    /// Returns the search text for API calls, converting whitespace-only input to null.
    /// </summary>
    protected string? SearchFilter =>
        string.IsNullOrWhiteSpace(SearchText) ? null : SearchText;
}

/// <summary>
/// Base class for tabbed ViewModels that combine multiple data views with pagination.
/// Provides ActiveTab switching with automatic data reload.
/// </summary>
public abstract partial class TabbedPagedViewModel : PagedViewModel
{
    [ObservableProperty] private string _activeTab = string.Empty;

    [RelayCommand]
    protected async Task SwitchTabAsync(string tab)
    {
        ActiveTab = tab;
        CurrentPage = 1;
        await LoadAsync();
    }
}

/// <summary>
/// Base class for dialog/edit ViewModels with common save-and-close-window pattern.
/// Eliminates the repeated Dispatcher.Invoke window-closing code across edit ViewModels.
/// </summary>
public abstract partial class DialogViewModel : BaseViewModel
{
    public bool Saved { get; protected set; }

    /// <summary>
    /// Closes the window whose DataContext is this ViewModel.
    /// </summary>
    protected void CloseDialog()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            foreach (Window w in Application.Current.Windows)
            {
                if (w.DataContext == this)
                {
                    w.Close();
                    break;
                }
            }
        });
    }

    /// <summary>
    /// Marks the dialog as saved and closes the window.
    /// </summary>
    protected void SaveAndClose()
    {
        Saved = true;
        CloseDialog();
    }
}
