namespace CoreLib.Common;

public class BadResponse
{
    public int StatusCode { get; set; }
    public string Message { get; set; }
    public Dictionary<string, List<string>> Errors { get; set; }
}