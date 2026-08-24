using System.Net.Http.Json;
using CoreLib.Dashboards;
using Web.Client.Interfaces;
using Web.Client.Shared.Extensions;

namespace Web.Client.Feature.Dashboards;

public class DashboardService(HttpClient _httpClient) : IDashboard
{
    public async Task<DashboardData> Get(CancellationToken ct, string? UserId = "")
    {
        var data = await _httpClient.GetFromJsonAsync<DashboardData>("/api/DashboardData", ct);
        return data;
    }

    public async Task<DosenDashboardData> GetDosen(CancellationToken ct, string? UserId = "")
    {
        var data = await _httpClient.GetFromJsonAsync<DosenDashboardData>("/api/DashboardDataDosen", ct);
        return data;
    }

    public async Task<AdminDashboardData> GetAdmin(CancellationToken ct)
    {
        var data = await _httpClient.GetFromJsonAsyncWithNewtonsoft<AdminDashboardData>("/api/DashboardDataAdmin", ct);
        return data;
    }
}