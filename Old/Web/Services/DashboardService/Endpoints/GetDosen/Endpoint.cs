using System.Security.Claims;
using CoreLib.Dashboards;

namespace Web.Services.DashboardService.Endpoints.GetDosen;

public class Endpoint : EndpointWithoutRequest<DosenDashboardData>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IDashboard _repo;

    public Endpoint(IDashboard repo, IHttpContextAccessor httpContextAccessor)
    {
        _repo = repo;
        _httpContextAccessor = httpContextAccessor;
    }

    public override void Configure()
    {
        Get("/DashboardDataDosen");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userID = _httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var res = await _repo.GetDosen(ct, userID);
        await SendAsync(res, cancellation: ct);
    }
}