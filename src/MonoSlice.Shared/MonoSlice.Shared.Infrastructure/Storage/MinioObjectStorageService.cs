using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MonoSlice.Shared.Abstractions.Storage;

namespace MonoSlice.Shared.Infrastructure.Storage;

public class MinioObjectStorageService : IObjectStorageService
{
    private readonly IAmazonS3 _s3Client;
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
        }
        else
        {
            var serviceUrl = (_settings.UseSSL ? "https://" : "http://") + _settings.Endpoint;
            var config = new AmazonS3Config
            {
                ServiceURL = serviceUrl,
                ForcePathStyle = true,
                UseHttp = !_settings.UseSSL
            };

            _s3Client = new AmazonS3Client(_settings.AccessKey, _settings.SecretKey, config);
        }
    }

    public Task<string> GeneratePresignedUploadUrlAsync(
        string bucket, 
        string objectKey, 
        TimeSpan expiry, 
        string contentType)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = bucket,
            Key = objectKey,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.Add(expiry),
            ContentType = contentType,
            Protocol = _settings.UseSSL ? Protocol.HTTPS : Protocol.HTTP
        };

        var url = _s3Client.GetPreSignedURL(request);
        url = NormalizeUrlWithPublicEndpoint(url);

        _logger.LogDebug("Generated presigned upload URL for bucket '{Bucket}', key '{Key}'", bucket, objectKey);
        return Task.FromResult(url);
    }

    public Task<string> GeneratePresignedDownloadUrlAsync(
        string bucket, 
        string objectKey, 
        TimeSpan expiry)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = bucket,
            Key = objectKey,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(expiry),
            Protocol = _settings.UseSSL ? Protocol.HTTPS : Protocol.HTTP
        };

        var url = _s3Client.GetPreSignedURL(request);
        url = NormalizeUrlWithPublicEndpoint(url);

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

    private string NormalizeUrlWithPublicEndpoint(string url)
    {
        if (!string.IsNullOrWhiteSpace(_settings.PublicEndpoint) && Uri.TryCreate(_settings.PublicEndpoint, UriKind.Absolute, out var publicUri) && Uri.TryCreate(url, UriKind.Absolute, out var generatedUri))
        {
            var builder = new UriBuilder(generatedUri)
            {
                Scheme = publicUri.Scheme,
                Host = publicUri.Host,
                Port = publicUri.IsDefaultPort ? -1 : publicUri.Port
            };
            return builder.Uri.ToString();
        }

        if (!_settings.UseSSL && url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return "http://" + url.Substring("https://".Length);
        }

        return url;
    }
}
