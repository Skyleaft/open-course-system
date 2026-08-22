using Web.Client.Feature.UserManagements;
using Web.Client.Shared.Models;

namespace Web.Services.UserServices.Endpoints.Update;

public class Endpoint(IUser repo) : Endpoint<UserEditDTO, ServiceResponse>
{
    public override void Configure()
    {
        Put("/user/{Id}");
    }

    public override async Task HandleAsync(UserEditDTO r, CancellationToken ct)
    {
        var res = await repo.Update(r, ct);
        await SendAsync(res, cancellation: ct);
    }
}