using CoreLib.Common;

namespace Web.Services.UserServices.Endpoints.Find;

public class Endpoint(IUser repo) : Endpoint<FindRequest, PaginatedResponse<ApplicationUser>>
{
    public override void Configure()
    {
        Post("/user/Find");
    }

    public override async Task HandleAsync(FindRequest r, CancellationToken ct)
    {
        var res = await repo.Find(r, ct);
        await SendAsync(res, cancellation: ct);
    }
}