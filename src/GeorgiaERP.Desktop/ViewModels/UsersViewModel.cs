using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using GeorgiaERP.Desktop.Models;
using GeorgiaERP.Desktop.Services;

namespace GeorgiaERP.Desktop.ViewModels;

public partial class UsersViewModel : PagedViewModel
{
    private readonly IUserService _userService;

    [ObservableProperty] private UserListDto? _selectedUser;

    public ObservableCollection<UserListDto> Users { get; } = [];

    public UsersViewModel(IUserService userService)
    {
        _userService = userService;
    }

    protected override async Task LoadCoreAsync()
    {
        var result = await _userService.GetUsersAsync(
            SearchFilter, page: CurrentPage);
        if (result is null) return;

        ReplaceItems(Users, result.Items);
        TotalCount = result.TotalCount;
        TotalPages = result.TotalPages;
    }
}
