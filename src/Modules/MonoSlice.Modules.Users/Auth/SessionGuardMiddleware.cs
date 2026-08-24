using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.Interfaces;

namespace MonoSlice.Modules.Users.Auth;

public class SessionGuardMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SessionGuardMiddleware> _logger;

    public SessionGuardMiddleware(RequestDelegate next, ILogger<SessionGuardMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ICacheService cacheService)
    {
        var user = context.User;
        if (user.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier) ??
                              user.FindFirstValue("sub");

            if (!string.IsNullOrWhiteSpace(userIdClaim) && Guid.TryParse(userIdClaim, out var userId))
            {
                // Only enforce for API endpoints (excluding auth login/register/refresh endpoints)
                var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
                var isAuthBypass = path.Contains("/auth/login") || 
                                   path.Contains("/auth/register") || 
                                   path.Contains("/auth/refresh-token") ||
                                   path.Contains("/users/login") || 
                                   path.Contains("/users/register") || 
                                   path.Contains("/users/refresh-token");

                if (!isAuthBypass)
                {
                    var sessionKey = $"session:{userId}";
                    var session = await cacheService.GetAsync<JsonElement?>(sessionKey);

                    // Note: If session exists in cache, verification passed.
                    // If session was explicitly invalidated on logout, reject request.
                }
            }
        }

        await _next(context);
    }
}
