using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace MonoSlice.Modules.Users.Auth;

public sealed class CompositeAuthMiddleware
{
    private readonly RequestDelegate _next;

    public CompositeAuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            var authHeader = context.Request.Headers.Authorization.ToString();
            if (!string.IsNullOrWhiteSpace(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                var jwtResult = await context.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);
                if (jwtResult.Succeeded && jwtResult.Principal is not null)
                {
                    context.User = jwtResult.Principal;
                }
            }
            else
            {
                var cookieResult = await context.AuthenticateAsync(IdentityConstants.ApplicationScheme);
                if (cookieResult.Succeeded && cookieResult.Principal is not null)
                {
                    context.User = cookieResult.Principal;
                }
            }
        }

        await _next(context);
    }
}
