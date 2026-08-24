using CoreLib.RoomSet;

namespace Web.Services.RoomServices.Endpoints.AllRefference;

public class Endpoint : EndpointWithoutRequest<IEnumerable<Room>>
{
    private readonly IRoom _repo;

    public Endpoint(IRoom examService)
    {
        _repo = examService;
    }

    public override void Configure()
    {
        Get("/room/AllRefference");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var res = await _repo.AllRefference(ct);
        await SendAsync(res, cancellation: ct);
    }
}