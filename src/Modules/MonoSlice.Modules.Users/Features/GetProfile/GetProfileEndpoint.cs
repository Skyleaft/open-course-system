using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MonoSlice.Modules.Users.Features.Register;
using MonoSlice.Shared.Abstractions.Common;

namespace MonoSlice.Modules.Users.Features.GetProfile;

public static class GetProfileEndpoint
{
    public static void MapGetProfileEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/me", async (
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new GetProfileQuery(), cancellationToken);
            return Results.Ok(result);
        })
        .WithName("GetCurrentUserProfile")
        .WithSummary("Retrieves the currently authenticated user's profile")
        .Produces<ApiResponse<UserResponseDto>>(StatusCodes.Status200OK)
        .Produces<ApiResponse>(StatusCodes.Status401Unauthorized)
        .RequireAuthorization();
    }
}
