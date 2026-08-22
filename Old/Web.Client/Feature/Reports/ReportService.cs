using System.Net.Http.Json;
using CoreLib.Common;
using CoreLib.Report;
using Web.Client.Interfaces;

namespace Web.Client.Feature.Reports;

public class ReportService(HttpClient _httpClient) : IReport
{
    public async Task<ExamReport> Get(Guid Id, CancellationToken ct)
    {
        var data = await _httpClient.GetFromJsonAsync<ExamReport>($"/api/report/{Id}", ct);
        return data;
    }

    public async Task<PaginatedResponse<ExamReport>> Find(FindRequest r, CancellationToken ct)
    {
        var res = await _httpClient.PostAsJsonAsync("api/report/find", r, ct);
        var data = new PaginatedResponse<ExamReport>();
        if (res.IsSuccessStatusCode) data = await res.Content.ReadFromJsonAsync<PaginatedResponse<ExamReport>>(ct);
        return data;
    }
}