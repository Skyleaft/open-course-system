namespace CoreLib.Common;

public class FindRequest
{
    public string Search { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public string OrderBy { get; set; }
    public int Direction { get; set; }
    public string? Filter { get; set; } = "";
}