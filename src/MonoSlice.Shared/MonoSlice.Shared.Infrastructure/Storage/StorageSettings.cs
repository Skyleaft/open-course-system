namespace MonoSlice.Shared.Infrastructure.Storage;

public class StorageSettings
{
    public const string SectionName = "Minio";

    public string Endpoint { get; set; } = "localhost:9000";
    public string? PublicEndpoint { get; set; }
    public string AccessKey { get; set; } = "minioadmin";
    public string SecretKey { get; set; } = "minioadmin123";
    public bool UseSSL { get; set; } = false;
    public string Region { get; set; } = "us-east-1";
}
