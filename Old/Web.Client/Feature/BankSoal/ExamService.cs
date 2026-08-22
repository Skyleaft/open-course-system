using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CoreLib.BankSoal;
using CoreLib.Common;
using Web.Client.Interfaces;
using Web.Client.Shared.Models;

namespace Web.Client.Feature.BankSoal;

public class ExamService(HttpClient _httpClient) : IExam
{
    public async Task<CreatedResponse<Exam>> Create(Exam r, CancellationToken ct)
    {
        var res = await _httpClient.PostAsJsonAsync("api/exam/", r, ct);
        if (res.IsSuccessStatusCode)
        {
            var createdUser = await res.Content.ReadFromJsonAsync<CreatedResponse<Exam>>(ct);
            return createdUser;
        }

        return new CreatedResponse<Exam>(false, res.ReasonPhrase);
    }

    public async Task<ServiceResponse> Update(Exam r, CancellationToken ct)
    {
        var res = await _httpClient.PutAsJsonAsync($"api/exam/{r.Id}", r, ct);
        if (res.IsSuccessStatusCode)
        {
            var content = await res.Content.ReadFromJsonAsync<ServiceResponse>(ct);
            return content;
        }

        if (res.StatusCode == HttpStatusCode.BadRequest)
        {
            var content = await res.Content.ReadFromJsonAsync<BadResponse>(ct);
            return new ServiceResponse(false, JsonSerializer.Serialize(content.Errors));
        }

        return new ServiceResponse(false, res.ReasonPhrase);
    }

    public async Task<ServiceResponse> Delete(int Id)
    {
        var res = await _httpClient.DeleteFromJsonAsync<ServiceResponse>($"api/exam/{Id}");
        return res;
    }

    public async Task<Exam> Get(int Id)
    {
        var data = await _httpClient.GetFromJsonAsync<Exam>($"/api/exam/{Id}");
        return data;
    }

    public async Task<PaginatedResponse<Exam>> Find(FindRequest r, CancellationToken ct)
    {
        var res = await _httpClient.PostAsJsonAsync("api/exam/find", r, ct);
        var data = new PaginatedResponse<Exam>();
        if (res.IsSuccessStatusCode) data = await res.Content.ReadFromJsonAsync<PaginatedResponse<Exam>>(ct);
        return data;
    }
}