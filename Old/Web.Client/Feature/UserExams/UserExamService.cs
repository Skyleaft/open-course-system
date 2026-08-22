using System.Net;
using System.Net.Http.Json;
using System.Text;
using CoreLib.Common;
using CoreLib.TakeExam;
using Newtonsoft.Json;
using Web.Client.Interfaces;
using Web.Client.Shared.Models;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Web.Client.Feature.UserExams;

public class UserExamService(HttpClient _httpClient) : IUserExam
{
    public async Task<CreatedResponse<UserExam>> Create(CreateUserExamDTO r, CancellationToken ct)
    {
        var jsonContent = JsonConvert.SerializeObject(r);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
        var request = await _httpClient.PostAsync("api/userexam/", content, ct);

        if (request.IsSuccessStatusCode)
        {
            var created = await request.Content.ReadFromJsonAsync<CreatedResponse<UserExam>>(ct);
            return created;
        }

        var error = await request.Content.ReadFromJsonAsync<BadResponse>(ct);
        return new CreatedResponse<UserExam>(false, error.Errors["generalErrors"].FirstOrDefault());
    }

    public async Task<ServiceResponse> Update(UserExam r, CancellationToken ct)
    {
        var jsonContent = JsonConvert.SerializeObject(r);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
        var request = await _httpClient.PutAsync($"api/userexam/{r.Id}", content, ct);
        if (request.IsSuccessStatusCode)
        {
            var data = await request.Content.ReadFromJsonAsync<ServiceResponse>(ct);
            return data;
        }

        if (request.StatusCode == HttpStatusCode.BadRequest)
        {
            var data = await request.Content.ReadFromJsonAsync<BadResponse>(ct);
            return new ServiceResponse(false, JsonSerializer.Serialize(data.Errors));
        }

        return new ServiceResponse(false, request.ReasonPhrase);
    }

    public async Task<ServiceResponse> Delete(Guid Id)
    {
        var res = await _httpClient.DeleteFromJsonAsync<ServiceResponse>($"api/userexam/{Id}");
        return res;
    }

    public async Task<UserExam> Get(Guid Id, CancellationToken ct)
    {
        var data = await _httpClient.GetFromJsonAsync<UserExam>($"/api/userexam/{Id}", ct);
        return data;
    }

    public async Task<UserExam> GetOnly(Guid Id)
    {
        var data = await _httpClient.GetFromJsonAsync<UserExam>($"/api/userexam/getonly/{Id}");
        return data;
    }

    public async Task<IEnumerable<UserExam>> GetAll(Guid RoomId, CancellationToken ct)
    {
        var data = await _httpClient.GetFromJsonAsync<IEnumerable<UserExam>>($"/api/userexam/get-all/{RoomId}");
        return data;
    }

    public async Task<PaginatedResponse<UserExam>> Find(FindRequest r, CancellationToken ct, string? UserId = "")
    {
        var res = await _httpClient.PostAsJsonAsync("api/userexam/find", r, ct);
        if (res.IsSuccessStatusCode)
        {
            var json = await res.Content.ReadAsStringAsync(ct);
            return JsonConvert.DeserializeObject<PaginatedResponse<UserExam>>(json) ??
                   throw new InvalidOperationException();
        }

        return null;
    }

    public async Task<ServiceResponse> UpdateJawaban(UpdateJawabanDTO r, CancellationToken ct)
    {
        var jsonContent = JsonConvert.SerializeObject(r);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
        var res = await _httpClient.PutAsync($"api/userexam/userAnswer/{r.UserAnswerId}", content, ct);
        if (res.IsSuccessStatusCode)
        {
            var data = await res.Content.ReadFromJsonAsync<ServiceResponse>(ct);
            return data;
        }

        if (res.StatusCode == HttpStatusCode.BadRequest)
        {
            var data = await res.Content.ReadFromJsonAsync<BadResponse>(ct);
            return new ServiceResponse(false, JsonSerializer.Serialize(data.Errors));
        }

        return new ServiceResponse(false, res.ReasonPhrase);
    }

    public async Task<List<UserAnswer>> GetUserAnswers(Guid UserExamId, CancellationToken ct)
    {
        var data = await _httpClient.GetFromJsonAsync<List<UserAnswer>>($"/api/userexam/{UserExamId}/userAnswer", ct);
        return data;
    }

    public async Task<ServiceResponse> RetryExam(Guid UserExamId, CancellationToken ct)
    {
        var res = await _httpClient.GetAsync($"/api/userexam/retry/{UserExamId}", ct);
        if (res.IsSuccessStatusCode)
        {
            var data = await res.Content.ReadFromJsonAsync<ServiceResponse>(ct);
            return data;
        }

        return new ServiceResponse(false, res.ReasonPhrase);
    }

    public async Task<ServiceResponse> StartExam(Guid UserExamId, CancellationToken ct)
    {
        var res = await _httpClient.GetAsync($"/api/userexam/start/{UserExamId}", ct);
        if (res.IsSuccessStatusCode)
        {
            var data = await res.Content.ReadFromJsonAsync<ServiceResponse>(ct);
            return data;
        }

        return new ServiceResponse(false, res.ReasonPhrase);
    }
}