using CoreLib.RoomSet;
using Web.Client.Shared.Models;

namespace Web.Services.RoomServices.Endpoints.Update;

public class Endpoint : Endpoint<Room, ServiceResponse>
{
    private readonly IRoom _repo;

    public Endpoint(IRoom examService)
    {
        _repo = examService;
    }

    public override void Configure()
    {
        Put("/room/{Id}");
    }


    public override async Task HandleAsync(Room r, CancellationToken ct)
    {
        var res = await _repo.Update(r);
        await SendAsync(res, cancellation: ct);
    }
}