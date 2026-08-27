using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MonoSlice.Shared.Abstractions.Storage;

namespace MonoSlice.Shared.Infrastructure.Storage;

public class MinioObjectStorageService : IObjectStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly IAmazonS3 _signingS3Client;
    private readonly StorageSettings _settings;
    private readonly ILogger<MinioObjectStorageService> _logger;

    public MinioObjectStorageService(
        IOptions<StorageSettings> settings,
        ILogger<MinioObjectStorageService> logger,
        IAmazonS3? s3Client = null)
    {
        _settings = settings.Value;
        _logger = logger;

        if (s3Client != null)
        {
            _s3Client = s3Client;
            _signingS3Client = s3Client;
        }
        else
        {
            var serviceUrl = (_settings.UseSSL ? "https://" : "http://") + _settings.Endpoint;
            var config = new AmazonS3Config
            {
                ServiceURL = serviceUrl,
                ForcePathStyle = true,
                UseHttp = !_settings.UseSSL,
                AuthenticationRegion = _settings.Region
            };

            _s3Client = new AmazonS3Client(_settings.AccessKey, _settings.SecretKey, config);

            if (!string.IsNullOrWhiteSpace(_settings.PublicEndpoint) && Uri.TryCreate(_settings.PublicEndpoint, UriKind.Absolute, out var publicUri))
            {
                var isPublicHttps = publicUri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase);
                var signingConfig = new AmazonS3Config
                {
                    ServiceURL = _settings.PublicEndpoint.TrimEnd('/'),
                    ForcePathStyle = true,
                    UseHttp = !isPublicHttps,
                    AuthenticationRegion = _settings.Region
                };
                _signingS3Client = new AmazonS3Client(_settings.AccessKey, _settings.SecretKey, signingConfig);
            }
            else
            {
                _signingS3Client = _s3Client;
            }
        }
    }

    public Task<string> GeneratePresignedUploadUrlAsync(
        string bucket, 
        string objectKey, 
        TimeSpan expiry, 
        string contentType)
    {
        var isHttps = !string.IsNullOrWhiteSpace(_settings.PublicEndpoint)
            ? _settings.PublicEndpoint.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            : _settings.UseSSL;

        var request = new GetPreSignedUrlRequest
        {
            BucketName = bucket,
            Key = objectKey,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.Add(expiry),
            ContentType = contentType,
            Protocol = isHttps ? Protocol.HTTPS : Protocol.HTTP
        };

        var url = _signingS3Client.GetPreSignedURL(request);
        _logger.LogDebug("Generated presigned upload URL for bucket '{Bucket}', key '{Key}'", bucket, objectKey);
        return Task.FromResult(url);
    }

    public Task<string> GeneratePresignedDownloadUrlAsync(
        string bucket, 
        string objectKey, 
        TimeSpan expiry)
    {
        var isHttps = !string.IsNullOrWhiteSpace(_settings.PublicEndpoint)
            ? _settings.PublicEndpoint.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            : _settings.UseSSL;

        var request = new GetPreSignedUrlRequest
        {
            BucketName = bucket,
            Key = objectKey,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(expiry),
            Protocol = isHttps ? Protocol.HTTPS : Protocol.HTTP
        };

        var url = _signingS3Client.GetPreSignedURL(request);
        _logger.LogDebug("Generated presigned download URL for bucket '{Bucket}', key '{Key}'", bucket, objectKey);
        return Task.FromResult(url);
    }

    public async Task DeleteObjectAsync(
        string bucket, 
        string objectKey, 
        CancellationToken ct = default)
    {
        try
        {
            var request = new DeleteObjectRequest
            {
                BucketName = bucket,
                Key = objectKey
            };

            await _s3Client.DeleteObjectAsync(request, ct);
            _logger.LogInformation("Deleted object from bucket '{Bucket}', key '{Key}'", bucket, objectKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete object from bucket '{Bucket}', key '{Key}'", bucket, objectKey);
            throw;
        }
    }

    public async Task UploadObjectAsync(
        string bucket, 
        string objectKey, 
        Stream content, 
        string contentType, 
        CancellationToken ct = default)
    {
        try
        {
            var request = new PutObjectRequest
            {
                BucketName = bucket,
                Key = objectKey,
                InputStream = content,
                ContentType = contentType
            };

            await _s3Client.PutObjectAsync(request, ct);
            _logger.LogInformation("Uploaded object to bucket '{Bucket}', key '{Key}'", bucket, objectKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload object to bucket '{Bucket}', key '{Key}'", bucket, objectKey);
            throw;
        }
    }

    public async Task<Stream?> GetObjectAsync(
        string bucket, 
        string objectKey, 
        CancellationToken ct = default)
    {
        try
        {
            var request = new GetObjectRequest
            {
                BucketName = bucket,
                Key = objectKey
            };

            var response = await _s3Client.GetObjectAsync(request, ct);
            return response.ResponseStream;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Object not found in bucket '{Bucket}', key '{Key}'", bucket, objectKey);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get object from bucket '{Bucket}', key '{Key}'", bucket, objectKey);
            throw;
        }
    }

    public async Task<bool> CheckHealthAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _s3Client.ListBucketsAsync(ct);
            return response.HttpStatusCode == System.Net.HttpStatusCode.OK;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MinIO health check failed connecting to endpoint '{Endpoint}'", _settings.Endpoint);
            return false;
        }
    }

    public async Task<IReadOnlyDictionary<string, bool>> CheckBucketsHealthAsync(
        IEnumerable<string> buckets, 
        CancellationToken ct = default)
    {
        var results = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        var bucketList = buckets.ToList();
        try
        {
            var list = await _s3Client.ListBucketsAsync(ct);
            var existing = new HashSet<string>(list.Buckets.Select(b => b.BucketName), StringComparer.OrdinalIgnoreCase);

            foreach (var b in bucketList)
            {
                results[b] = existing.Contains(b);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check MinIO buckets health status.");
            foreach (var b in bucketList)
            {
                results[b] = false;
            }
        }
        return results;
    }
}
