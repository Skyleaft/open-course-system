using Blazored.LocalStorage;
using Microsoft.Extensions.Caching.Memory;
using Web.Common.Database;

namespace Web.Services.ReferenceServices;

public class ReferenceService : IReferences
{
    private readonly AppDbContext _appDbContext;
    private readonly ILocalStorageService _storageService;
    private readonly MemoryCache cache = new(new MemoryCacheOptions());

    public ReferenceService(AppDbContext appDbContext, ILocalStorageService storageService)
    {
        _appDbContext = appDbContext;
        _storageService = storageService;
    }

    public async Task<List<Provinsi>> GetAllprovinsi()
    {
        var key = "ref-provinsi";
        var data = await _storageService.GetItemAsync<List<Provinsi>>(key);
        if (data == null)
        {
            var fetch = _appDbContext.Provinsi.ToList();
            var fetkota = _appDbContext.Kota.ToList();
            cache.Set(key, fetch, TimeSpan.FromDays(1));
            cache.Set("ref-kota", fetkota, TimeSpan.FromDays(1));
            data = fetch;
            try
            {
                await _storageService.SetItemAsync(key, fetch);
                await _storageService.SetItemAsync("ref-kota", fetkota);
            }
            catch (Exception e)
            {
                return data;
            }
        }

        return data;
    }

    public async Task<List<Kota>> GetAllKota()
    {
        var key = "ref-kota";
        var data = cache.Get<List<Kota>>(key);
        if (data == null)
        {
            var fetch = _appDbContext.Kota.ToList();
            cache.Set(key, fetch, TimeSpan.FromDays(1));
            data = fetch;
        }

        return data;
    }
}