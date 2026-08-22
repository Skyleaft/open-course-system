using Web.Client.Shared.Models;

namespace Web.Services.UserServices.Endpoints.Activate;

public class Endpoint : EndpointWithoutRequest<ServiceResponse>
{
    private readonly IUser _repo;

    public Endpoint(IUser examService)
    {
        _repo = examService;
    }

    public override void Configure()
    {
        Get("/user/activate/{Id}");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var res = await _repo.ForceActivate(Route<string>("Id"));
        await SendAsync(res, cancellation: ct);
    }
}