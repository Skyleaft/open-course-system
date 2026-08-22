using CoreLib.Common;
using Web.Client.Feature.UserManagements;

namespace Web.Services.UserServices.Endpoints.Register;

public class Endpoint(IUser repo) : Endpoint<UserAddDTO, CreatedResponse<UserDTO>>
{
    public override void Configure()
    {
        Post("/user/register");
    }

    public override async Task HandleAsync(UserAddDTO r, CancellationToken ct)
    {
        var res = await repo.Register(r, ct);
        if (res.isSuccess)
            await SendCreatedAtAsync($"/user/{res.Data.Id}", res.Data, res, cancellation: ct);
        else
            ThrowError(res.Message);
    }
}