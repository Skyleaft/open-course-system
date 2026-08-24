namespace Web.Client.Shared.Models;

public class ErrorResult
{
    public int StatusCode { get; set; }
    public string Message { get; set; }
    public Errors Errors { get; set; }
}

public class Errors
{
    public List<string> GeneralErrors { get; set; }
}