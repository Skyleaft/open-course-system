using Blazored.LocalStorage;
using Blazored.SessionStorage;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using MudExtensions.Services;
using Web.Client;
using Web.Client.Services;
using Web.Client.Services.Notifications;
using Web.Client.Services.UserPreferences;
using Web.Client.Shared.Extensions;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthenticationStateDeserialization();
builder.Services.AddSingleton<OnlineUserStateService>();

builder.Services.AddBlazoredLocalStorage();
builder.Services.AddBlazoredSessionStorage();

builder.Services.AddHttpClient("ServerAPI",
    client => { client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress); }
);
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>()
    .CreateClient("ServerAPI"));

builder.Services.InjectService();
builder.Services.AddScoped<IUserPreferencesService, UserPreferencesService>();
builder.Services.AddScoped<INotificationService, InMemoryNotificationService>();
builder.Services.AddScoped<LayoutService>();
builder.Services.AddScoped<Navigation>();

builder.Services.AddMudBlazorSnackbar();
builder.Services.AddMudBlazorResizeListener();
builder.Services.AddMudBlazorResizeObserver();
builder.Services.AddMudBlazorResizeObserverFactory();
builder.Services.AddMudPopoverService();
builder.Services.AddMudServices();
builder.Services.AddMudExtensions();
await builder.Build().RunAsync();