using Web.Client.Shared.Models;

namespace Web.Services.UserServices.Endpoints.ResetPassword;

public class Endpoint(IUser repo) : Endpoint<PasswordReset, ServiceResponse>
{
    public override void Configure()
    {
        Post("/user/resetPassword");
    }

    public override async Task HandleAsync(PasswordReset r, CancellationToken ct)
    {
        var res = await repo.ResetPassword(r);
        await SendAsync(res, cancellation: ct);
    }
}