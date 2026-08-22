using Newtonsoft.Json;

namespace Web.Client.Shared.Extensions;

public static class HttpClientExtensions
{
    public static async Task<T> GetFromJsonAsyncWithNewtonsoft<T>(this HttpClient client, string requestUri,
        CancellationToken ct)
    {
        var responseString = await client.GetStringAsync(requestUri, ct);
        return JsonConvert.DeserializeObject<T>(responseString);
    }
}