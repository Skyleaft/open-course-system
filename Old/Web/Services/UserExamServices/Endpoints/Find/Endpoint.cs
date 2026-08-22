using System.Security.Claims;
using CoreLib.Common;
using CoreLib.TakeExam;

namespace Web.Services.UserExamServices.Endpoints.Find;

public class Endpoint : Endpoint<FindRequest, PaginatedResponse<UserExam>>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUserExam _repo;

    public Endpoint(IUserExam examService, IHttpContextAccessor httpContextAccessor)
    {
        _repo = examService;
        _httpContextAccessor = httpContextAccessor;
    }

    public override void Configure()
    {
        Post("/UserExam/Find");
    }

    public override async Task HandleAsync(FindRequest r, CancellationToken ct)
    {
        var userID = _httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var res = await _repo.Find(r, ct, userID);
        await SendAsync(res, cancellation: ct);
    }
}