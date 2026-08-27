namespace MonoSlice.Shared.Abstractions.Storage;

public interface IObjectStorageService
{
    Task<string> GeneratePresignedUploadUrlAsync(
        string bucket, 
        string objectKey, 
        TimeSpan expiry, 
        string contentType);

    Task<string> GeneratePresignedDownloadUrlAsync(
        string bucket, 
        string objectKey, 
        TimeSpan expiry);

    Task DeleteObjectAsync(
        string bucket, 
        string objectKey, 
        CancellationToken ct = default);

    Task UploadObjectAsync(
        string bucket, 
        string objectKey, 
        Stream content, 
        string contentType, 
        CancellationToken ct = default);

    Task<Stream?> GetObjectAsync(
        string bucket, 
        string objectKey, 
        CancellationToken ct = default);

    Task<bool> CheckHealthAsync(
        CancellationToken ct = default);

    Task<IReadOnlyDictionary<string, bool>> CheckBucketsHealthAsync(
        IEnumerable<string> buckets, 
        CancellationToken ct = default);
}
