using Web.Client.Shared.Models;

namespace Web.Services.RoomServices.Endpoints.Delete;

public class Endpoint : EndpointWithoutRequest<ServiceResponse>
{
    private readonly IRoom _repo;

    public Endpoint(IRoom examService)
    {
        _repo = examService;
    }

    public override void Configure()
    {
        Delete("/room/{Id}");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var res = await _repo.Delete(Route<Guid>("Id"));
        await SendAsync(res, cancellation: ct);
    }
}