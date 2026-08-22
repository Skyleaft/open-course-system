using System.Net.Http.Headers;
using System.Net.Http.Json;
using CoreLib.Common;
using Microsoft.AspNetCore.Components;
using Web.Client.Interfaces;

namespace Web.Client.Feature.BankSoal;

public class DocxUploadService(HttpClient _httpClient, NavigationManager navigationManager):IDocx
{
    public async Task<DocxResult> ProcessDocxAsync(Stream stream, string fileName)
    {
        using var form = new MultipartFormDataContent();
        // Create content from stream
        var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        form.Add(fileContent, "File", fileName); // name, filename
        
        var res = await _httpClient.PostAsync("api/exam/upload", form);
        if (res.IsSuccessStatusCode)
        {
            var data = await res.Content.ReadFromJsonAsync<DocxResult>();
            return data;
        }
        throw new Exception("Upload Failed");
    }

    public async Task<string> GetDownloadUrl(string fileName)
    {
        return navigationManager.BaseUri + $"files/{fileName}";
    }
}