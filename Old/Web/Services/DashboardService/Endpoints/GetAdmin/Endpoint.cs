using CoreLib.Dashboards;

namespace Web.Services.DashboardService.Endpoints.GetAdmin;

public class Endpoint : EndpointWithoutRequest<AdminDashboardData>
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
        Get("/DashboardDataAdmin");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var res = await _repo.GetAdmin(ct);
        await SendAsync(res, cancellation: ct);
    }
}