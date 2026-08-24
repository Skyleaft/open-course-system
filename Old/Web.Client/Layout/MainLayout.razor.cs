using Blazored.SessionStorage;
using CoreLib.Common;
using CoreLib.Users;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using MudBlazor;
using Web.Client.Services;
using Web.Client.Shared.Extensions;
using Web.Client.Shared.Models;
using Web.Client.Shared.Theme;

namespace Web.Client.Layout;

public partial class MainLayout : LayoutComponentBase, IDisposable, IAsyncDisposable
{
    private readonly string avatarSource = "";

    private bool _drawerOpen = true;
    private MudThemeProvider _mudThemeProvider;
    private NavMenu _navMenuRef;
    private bool _topMenuOpen;
    private ApplicationUser CurrentUser;
    private HubConnection? hubConnection;

    private bool isConnected;
    private bool isLoaded;

    [CascadingParameter] private Task<AuthenticationState>? authenticationState { get; set; }

    [Inject] private LayoutService LayoutService { get; set; }
    [Inject] private NavigationManager navManager { get; set; }
    [Inject] private ISnackbar Snackbar { get; set; }
    [Inject] private ISessionStorageService _sessionStorageService { get; set; }
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; }

    public async ValueTask DisposeAsync()
    {
        if (hubConnection is not null)
        {
            await hubConnection.StopAsync();
            await hubConnection.DisposeAsync();
        }
    }

    public void Dispose()
    {
        LayoutService.MajorUpdateOccurred -= LayoutServiceOnMajorUpdateOccured;
    }

    protected override async Task OnInitializedAsync()
    {
        LayoutService.SetBaseTheme(Themes.LandingPageTheme());
        Snackbar.Configuration.PositionClass = Defaults.Classes.Position.BottomRight;
        LayoutService.MajorUpdateOccurred += LayoutServiceOnMajorUpdateOccured;

        await base.OnInitializedAsync();
    }


    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender && !isConnected)
        {
            await Task.Delay(100);
            isConnected = true;
            isLoaded = false;
            await ApplyUserPreferences();
            await _mudThemeProvider.WatchSystemPreference(OnSystemPreferenceChanged);
            isLoaded = true;
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            if (user.Identity is not null && user.Identity.IsAuthenticated)
            {
                var deviceInfo = await JS.InvokeAsync<string>("userInfo.getDeviceInfo");
                GeolocationResult? location = null;
                try
                {
                    location = await JS.InvokeAsync<GeolocationResult>("userInfo.getLocation");
                    Console.WriteLine($"Location : {location.Latitude} {location.Longitude}");
                }
                catch
                {
                    location = new GeolocationResult { Latitude = 0, Longitude = 0 };
                }

                hubConnection = new HubConnectionBuilder()
                    .WithUrl(
                        Navigation.ToAbsoluteUri(
                            $"/presenceHub?device={Uri.EscapeDataString(deviceInfo)}&lat={location.Latitude}&lng={location.Longitude}&user={user.Identity.Name}"),
                        options =>
                        {
                            options.HttpMessageHandlerFactory = innerHandler =>
                                new IncludeRequestCredentialsMessageHandler { InnerHandler = innerHandler };
                        })
                    .WithAutomaticReconnect()
                    .Build();

                hubConnection.On<List<OnlineUser>>("UserListUpdated", users =>
                {
                    OnlineUserStateService.UpdateOnlineUsers(users);
                    InvokeAsync(StateHasChanged);
                });

                await hubConnection.StartAsync();
            }

            StateHasChanged();
        }
    }

    private void ToggleDrawer()
    {
        _drawerOpen = !_drawerOpen;
    }

    private void OpenTopMenu()
    {
        _topMenuOpen = true;
    }

    private void OnDrawerOpenChanged(bool value)
    {
        _topMenuOpen = false;
        _drawerOpen = value;
        StateHasChanged();
    }

    private async Task ApplyUserPreferences()
    {
        var defaultDarkMode = await _mudThemeProvider.GetSystemPreference();
        await LayoutService.ApplyUserPreferences(defaultDarkMode);
    }

    private async Task OnSystemPreferenceChanged(bool newValue)
    {
        await LayoutService.OnSystemPreferenceChanged(newValue);
    }

    private void LayoutServiceOnMajorUpdateOccured(object sender, EventArgs e)
    {
        StateHasChanged();
    }
}