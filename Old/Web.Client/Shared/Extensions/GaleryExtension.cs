namespace Web.Client.Shared.Extensions;

public class GaleryExtension
{
    private readonly IConfiguration _configuration;

    public GaleryExtension(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string SetImageURL(string imgUrl)
    {
        return _configuration.GetConnectionString("ApiConnection") + imgUrl;
    }
}