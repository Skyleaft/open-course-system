using System.Diagnostics;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Web.Client.Shared.Models;

namespace Web.Components.Auth;

public class PersistingAuthenticationStateProvider : ServerAuthenticationStateProvider, IDisposable
{
    private readonly IdentityOptions _options;
    private readonly PersistentComponentState _state;
    private readonly PersistingComponentStateSubscription _subscription;
    private Task<AuthenticationState>? _authenticationStateTask;

    public PersistingAuthenticationStateProvider(
        PersistentComponentState persistentComponentState,
        IOptions<IdentityOptions> optionsAccessor)
    {
        _options = optionsAccessor.Value;
        _state = persistentComponentState;
        AuthenticationStateChanged += OnAuthenticationStateChanged;
        _subscription = _state.RegisterOnPersisting(OnPersistingAsync, RenderMode.InteractiveWebAssembly);
    }

    public void Dispose()
    {
        _authenticationStateTask?.Dispose();
        AuthenticationStateChanged -= OnAuthenticationStateChanged;
        _subscription.Dispose();
    }

    private async Task OnPersistingAsync()
    {
        if (_authenticationStateTask is null)
            throw new UnreachableException($"Authentication state not set in {nameof(OnPersistingAsync)}().");

        var authenticationState = await _authenticationStateTask;
        var principal = authenticationState.User;


        if (principal.Identity?.IsAuthenticated == true)
        {
            var userId = principal.FindFirst(_options.ClaimsIdentity.UserIdClaimType)?.Value;
            var name = principal.Identity.Name;
            var email = principal.FindFirst(_options.ClaimsIdentity.EmailClaimType)?.Value;
            var role = principal.FindFirst(_options.ClaimsIdentity.RoleClaimType)?.Value;
            var secstamp = principal.FindFirst(_options.ClaimsIdentity.SecurityStampClaimType)?.Value;
            if (userId != null && name != null)
                _state.PersistAsJson(nameof(UserInfo), new UserInfo
                {
                    UserId = userId,
                    Name = name,
                    Email = email,
                    Role = role,
                    SecurityStamp = secstamp
                });
        }
    }

    private void OnAuthenticationStateChanged(Task<AuthenticationState> authenticationStateTask)
    {
        _authenticationStateTask = authenticationStateTask;
    }
}