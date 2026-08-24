namespace CoreLib.Common;

public class OnlineUser
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string IpAddress { get; set; }
    public string Device { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}