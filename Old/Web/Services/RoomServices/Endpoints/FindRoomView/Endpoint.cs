using CoreLib.Common;
using CoreLib.RoomSet;

namespace Web.Services.RoomServices.Endpoints.FindRoomView;

public class Endpoint : Endpoint<FindRequest, PaginatedResponse<RoomExam>>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IRoom _repo;

    public Endpoint(IRoom examService, IHttpContextAccessor httpContextAccessor)
    {
        _repo = examService;
        _httpContextAccessor = httpContextAccessor;
    }

    public override void Configure()
    {
        Post("/room/FindRoomView");
    }

    public override async Task HandleAsync(FindRequest r, CancellationToken ct)
    {
        var username = _httpContextAccessor.HttpContext.User.Identity.Name;
        var res = await _repo.FindRoomView(r, ct, username);
        await SendAsync(res, cancellation: ct);
    }
}