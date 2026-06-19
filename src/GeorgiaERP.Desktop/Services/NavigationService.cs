namespace GeorgiaERP.Desktop.Services;

public interface INavigationService
{
    string CurrentView { get; }
    void NavigateTo(string viewName);
    event Action<string>? ViewChanged;
}

public class NavigationService : INavigationService
{
    public string CurrentView { get; private set; } = "Dashboard";
    public event Action<string>? ViewChanged;

    public void NavigateTo(string viewName)
    {
        if (CurrentView == viewName) return;
        CurrentView = viewName;
        ViewChanged?.Invoke(viewName);
    }
}
