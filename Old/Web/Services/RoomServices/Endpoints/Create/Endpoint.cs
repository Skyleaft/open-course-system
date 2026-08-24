using CoreLib.Common;
using CoreLib.RoomSet;

namespace Web.Services.RoomServices.Endpoints.Create;

public class Endpoint : Endpoint<Room, CreatedResponse<Room>>
{
    private readonly IRoom _repo;

    public Endpoint(IRoom examService)
    {
        _repo = examService;
    }

    public override void Configure()
    {
        Post("/room");
    }

    public override async Task HandleAsync(Room r, CancellationToken ct)
    {
        var res = await _repo.Create(r);
        await SendCreatedAtAsync($"/Room/{res.Data.Id}", res.Data, res, cancellation: ct);
    }
}