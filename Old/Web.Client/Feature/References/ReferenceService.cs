using System.Net.Http.Json;
using Blazored.LocalStorage;
using CoreLib.Users;
using Web.Client.Interfaces;

namespace Web.Client.Feature.References;

public class ReferenceService : IReferences
{
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _storageService;


    public ReferenceService(HttpClient httpClient, ILocalStorageService storageService)
    {
        _httpClient = httpClient;
        _storageService = storageService;
    }

    public async Task<List<Provinsi>> GetAllprovinsi()
    {
        var data = await _storageService.GetItemAsync<List<Provinsi>>("ref-provinsi");
        if (data == null)
        {
            var fetch = await _httpClient.GetFromJsonAsync<List<Provinsi>>("api/ref/provinsi/all");
            await _storageService.SetItemAsync("ref-provinsi", fetch);
            data = fetch;
        }

        return data;
    }

    public async Task<List<Kota>> GetAllKota()
    {
        var data = await _storageService.GetItemAsync<List<Kota>>("ref-kota");
        if (data == null)
        {
            var fetch = await _httpClient.GetFromJsonAsync<List<Kota>>("api/ref/kota/all");
            await _storageService.SetItemAsync("ref-kota", fetch);
            data = fetch;
        }

        return data;
    }
}