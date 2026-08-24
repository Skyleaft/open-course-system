using Web.Client.Shared.Models;

namespace Web.Services.UserServices.Endpoints.Delete;

public class Endpoint(IUser repo) : EndpointWithoutRequest<ServiceResponse>
{
    public override void Configure()
    {
        Delete("/user/{Id}");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var res = await repo.Delete(Route<string>("Id"), ct);
        await SendAsync(res, cancellation: ct);
    }
}