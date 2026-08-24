using CoreLib.RoomSet;

namespace Web.Services.RoomServices.Endpoints.GetRoomOnly;

public class Endpoint : EndpointWithoutRequest<RoomView>
{
    private readonly IRoom _repo;

    public Endpoint(IRoom examService)
    {
        _repo = examService;
    }

    public override void Configure()
    {
        Get("/roomview/only/{Id}");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var res = await _repo.GetRoomOnly(Route<Guid>("Id"), ct);
        await SendAsync(res, cancellation: ct);
    }
}