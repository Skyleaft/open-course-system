using CoreLib.RoomSet;

namespace Web.Services.RoomServices.Endpoints.GetByKode;

public class Endpoint : Endpoint<string, Room>
{
    private readonly IRoom _repo;

    public Endpoint(IRoom examService)
    {
        _repo = examService;
    }

    public override void Configure()
    {
        Get("/room");
    }

    public override async Task HandleAsync(string kode, CancellationToken ct)
    {
        var param = Query<string>("kode");
        var res = await _repo.Get(param);
        await SendAsync(res, cancellation: ct);
    }
}