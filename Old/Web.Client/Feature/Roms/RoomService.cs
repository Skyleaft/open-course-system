using System.Net;
using System.Net.Http.Json;
using System.Text;
using CoreLib.Common;
using CoreLib.RoomSet;
using Newtonsoft.Json;
using Web.Client.Interfaces;
using Web.Client.Shared.Models;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Web.Client.Feature.Roms;

public class RoomService(HttpClient _httpClient) : IRoom
{
    public async Task<CreatedResponse<Room>> Create(Room r)
    {
        var jsonContent = JsonConvert.SerializeObject(r);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
        var res = await _httpClient.PostAsync("api/room/", content);
        if (res.IsSuccessStatusCode)
        {
            var created = await res.Content.ReadFromJsonAsync<CreatedResponse<Room>>();
            return created;
        }

        return new CreatedResponse<Room>(false, res.ReasonPhrase);
    }

    public async Task<ServiceResponse> Update(Room r)
    {
        var jsonContent = JsonConvert.SerializeObject(r);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
        var res = await _httpClient.PutAsync($"api/room/{r.Id}", content);
        if (res.IsSuccessStatusCode)
        {
            var data = await res.Content.ReadFromJsonAsync<ServiceResponse>();
            return data;
        }

        if (res.StatusCode == HttpStatusCode.BadRequest)
        {
            var data = await res.Content.ReadFromJsonAsync<BadResponse>();
            return new ServiceResponse(false, JsonSerializer.Serialize(data.Errors));
        }

        return new ServiceResponse(false, res.ReasonPhrase);
    }

    public async Task<ServiceResponse> Delete(Guid Id)
    {
        var res = await _httpClient.DeleteFromJsonAsync<ServiceResponse>($"api/room/{Id}");
        return res;
    }

    public async Task<Room> Get(Guid Id, CancellationToken ct)
    {
        var data = await _httpClient.GetFromJsonAsync<Room>($"/api/room/{Id}", ct);
        return data;
    }

    public async Task<Room> Get(string kode)
    {
        var res = await _httpClient.GetAsync($"/api/room?kode={kode}");
        if (res.IsSuccessStatusCode)
        {
            var data = await res.Content.ReadAsStringAsync();
            if (!string.IsNullOrEmpty(data))
                return await res.Content.ReadFromJsonAsync<Room>();
        }

        return null;
    }

    public async Task<RoomView> GetRoomOnly(Guid Id, CancellationToken ct)
    {
        var data = await _httpClient.GetFromJsonAsync<RoomView>($"/api/roomview/only/{Id}", ct);
        return data;
    }

    public async Task<PaginatedResponse<Room>> Find(FindRequest r, CancellationToken ct, string? Username)
    {
        var res = await _httpClient.PostAsJsonAsync("api/room/find", r, ct);
        var data = new PaginatedResponse<Room>();
        if (res.IsSuccessStatusCode) data = await res.Content.ReadFromJsonAsync<PaginatedResponse<Room>>(ct);

        return data;
    }

    public async Task<PaginatedResponse<RoomExam>> FindRoomView(FindRequest r, CancellationToken ct,
        string? Username = "")
    {
        var res = await _httpClient.PostAsJsonAsync("api/room/findroomview", r, ct);
        var data = new PaginatedResponse<RoomExam>();
        if (res.IsSuccessStatusCode) data = await res.Content.ReadFromJsonAsync<PaginatedResponse<RoomExam>>(ct);

        return data;
    }

    public async Task<IEnumerable<Room>> AllRefference(CancellationToken ct)
    {
        var res = await _httpClient.GetAsync("api/room/AllRefference", ct);
        var data = new List<Room>();
        if (res.IsSuccessStatusCode) data = await res.Content.ReadFromJsonAsync<List<Room>>(ct);

        return data;
    }
}