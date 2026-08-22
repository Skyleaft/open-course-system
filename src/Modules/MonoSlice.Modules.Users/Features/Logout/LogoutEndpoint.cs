using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MonoSlice.Shared.Abstractions.Interfaces;

namespace MonoSlice.Modules.Users.Features.Logout;

public static class LogoutEndpoint
{
    public static void MapLogoutEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/logout", async (
                IMediator mediator,
                ICurrentUser currentUser,
                CancellationToken ct) =>
            {
                var command = new LogoutCommand { UserId = currentUser.UserId };
                var response = await mediator.Send(command, ct);
                return Results.Json(response, statusCode: response.StatusCode);
            })
            .WithName("Logout")
            .WithSummary("Revoke active user session and logout")
            .RequireAuthorization(policy => policy.RequireRole("Student", "Instructor", "Admin"));
    }
}
