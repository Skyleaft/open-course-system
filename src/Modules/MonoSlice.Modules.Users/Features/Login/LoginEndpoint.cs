using Mediator;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using MonoSlice.Modules.Users.Auth;
using MonoSlice.Modules.Users.Domain;
using MonoSlice.Shared.Abstractions.Common;

namespace MonoSlice.Modules.Users.Features.Login;

public static class LoginEndpoint
{
    public static void MapLoginEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/login", async (
            LoginCommand command,
            IMediator mediator,
            HttpContext httpContext,
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            IOptions<AuthSettings> authSettings,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(command, cancellationToken);

            if (result.Success && authSettings.Value.EnableCookieAuth && result.Data is not null)
            {
                var user = await userManager.FindByIdAsync(result.Data.User.Id.ToString());
                if (user is not null)
                {
                    await signInManager.SignInAsync(user, isPersistent: command.RememberMe);
                }
            }

            return Results.Ok(result);
        })
        .WithName("LoginUser")
        .WithSummary("Authenticates a user and returns JWT + refresh token")
        .WithDescription("Accepts email/username and password. Issues JWT Bearer token and optional authentication cookie.")
        .Produces<ApiResponse<LoginResponseDto>>(StatusCodes.Status200OK)
        .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
        .AllowAnonymous();
    }
}
