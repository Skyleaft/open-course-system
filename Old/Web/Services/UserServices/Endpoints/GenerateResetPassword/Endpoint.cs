namespace Web.Services.UserServices.Endpoints.GenerateResetPassword;

public class Endpoint(IUser repo) : EndpointWithoutRequest<string>
{
    public override void Configure()
    {
        Get("/user/{Id}/generateReset");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var res = await repo.GenerateResetPassword(Route<string>("Id"));
        await SendAsync(res, cancellation: ct);
    }
}