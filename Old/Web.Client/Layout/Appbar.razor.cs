using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using Web.Client.Services;

namespace Web.Client.Layout;

public partial class Appbar
{
    private DialogOptions _dialogOptions = new() { Position = DialogPosition.TopCenter, NoHeader = true };
    private bool _searchDialogAutocompleteOpen;
    private bool _searchDialogOpen;
    private int _searchDialogReturnedItemsCount;
    public bool DisplaySearchBar { get; set; } = true;
    [Parameter] public EventCallback<MouseEventArgs> DrawerToggleCallback { get; set; }
    [Inject] private LayoutService LayoutService { get; set; }
    [Inject] private NavigationManager navManager { get; set; }
    [Inject] private AuthenticationStateProvider authStateProvicder { get; set; }
    [Parameter] public string avaSource { get; set; }

    public bool IsSearchDialogOpen
    {
        get => _searchDialogOpen;
        set
        {
            _searchDialogAutocompleteOpen = default;
            _searchDialogReturnedItemsCount = default;
            _searchDialogOpen = value;
        }
    }

    private async Task Logout()
    {
        // var customAuthStateProvider = (CustomAuthenticationStateProvider)authStateProvicder;
        // await customAuthStateProvider.UpdateAuthenticationState(null);
        navManager.NavigateTo("/Account/Login?isLogout=true", true);
    }

    private void OpenSearchDialog()
    {
        IsSearchDialogOpen = true;
    }
}