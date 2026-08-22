using CoreLib.Common;

namespace Web.Client.Interfaces;

public interface IDocx
{
    Task<DocxResult> ProcessDocxAsync(Stream stream, string fileName);
    Task<string> GetDownloadUrl(string fileName);
}