using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MonoSlice.Shared.Abstractions.Common;

namespace MonoSlice.Modules.Users.Features.RefreshToken;

public static class RefreshTokenEndpoint
{
    public static void MapRefreshTokenEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/refresh-token", async (
            RefreshTokenCommand command,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(command, cancellationToken);
            return Results.Ok(result);
        })
        .WithName("RefreshToken")
        .WithSummary("Refreshes an expired JWT access token")
        .WithDescription("Accepts the expired access token and valid refresh token, returning a new token pair.")
        .Produces<ApiResponse<RefreshTokenResponseDto>>(StatusCodes.Status200OK)
        .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
        .AllowAnonymous();
    }
}
