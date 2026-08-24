using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MonoSlice.Shared.Abstractions.Common;

namespace MonoSlice.Modules.Users.Features.Register;

public static class RegisterEndpoint
{
    public static void MapRegisterEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/register", async (RegisterCommand command, IMediator mediator, CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(command, cancellationToken);
            return Results.Ok(result);
        })
        .WithName("RegisterUser")
        .WithSummary("Registers a new user account")
        .WithDescription("Creates a new user with GuidV7 primary key and assigns default User role.")
        .Produces<ApiResponse<UserResponseDto>>(StatusCodes.Status200OK)
        .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse>(StatusCodes.Status422UnprocessableEntity)
        .AllowAnonymous();
    }
}
