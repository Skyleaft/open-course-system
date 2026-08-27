namespace MonoSlice.Shared.Abstractions.Storage;

public static class ObjectStorageExtensions
{
    public const string DefaultBrandingBucket = "branding-assets";

    /// <summary>
    /// Parses a storage URL, relative path, or key into bucket and object key.
    /// </summary>
    public static (string Bucket, string Key)? TryParseStoragePath(
        string? urlOrKey,
        string defaultBucket = DefaultBrandingBucket)
    {
        if (string.IsNullOrWhiteSpace(urlOrKey))
        {
            return null;
        }

        string rawPath;
        if (Uri.TryCreate(urlOrKey, UriKind.Absolute, out var uri))
        {
            rawPath = Uri.UnescapeDataString(uri.AbsolutePath).TrimStart('/');
        }
        else
        {
            var queryIdx = urlOrKey.IndexOf('?');
            var clean = queryIdx >= 0 ? urlOrKey[..queryIdx] : urlOrKey;
            rawPath = clean.TrimStart('/');
        }

        if (string.IsNullOrWhiteSpace(rawPath))
        {
            return null;
        }

        if (rawPath.StartsWith($"{defaultBucket}/", StringComparison.OrdinalIgnoreCase))
        {
            return (defaultBucket, rawPath[(defaultBucket.Length + 1)..]);
        }

        if (rawPath.StartsWith("courses/thumbnails/", StringComparison.OrdinalIgnoreCase) ||
            rawPath.StartsWith("courses/", StringComparison.OrdinalIgnoreCase) ||
            rawPath.StartsWith("branding/", StringComparison.OrdinalIgnoreCase))
        {
            return (defaultBucket, rawPath);
        }

        var slashIdx = rawPath.IndexOf('/');
        if (slashIdx > 0 && slashIdx < rawPath.Length - 1)
        {
            return (rawPath[..slashIdx], rawPath[(slashIdx + 1)..]);
        }

        return (defaultBucket, rawPath);
    }

    /// <summary>
    /// Deletes an object from object storage by URL or key.
    /// </summary>
    public static async Task DeleteObjectByUrlAsync(
        this IObjectStorageService storageService,
        string? urlOrKey,
        string defaultBucket = DefaultBrandingBucket,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(urlOrKey))
        {
            return;
        }

        var parsed = TryParseStoragePath(urlOrKey, defaultBucket);
        if (parsed.HasValue && !string.IsNullOrWhiteSpace(parsed.Value.Bucket) && !string.IsNullOrWhiteSpace(parsed.Value.Key))
        {
            await storageService.DeleteObjectAsync(parsed.Value.Bucket, parsed.Value.Key, cancellationToken);
        }
    }
}
