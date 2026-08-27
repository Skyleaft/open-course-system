using MonoSlice.Shared.Abstractions.Storage;
using NSubstitute;
using Xunit;

namespace MonoSlice.Modules.Catalog.Tests;

public class ObjectStorageExtensionsTests
{
    [Theory]
    [InlineData("http://localhost:9000/branding-assets/courses/thumbnails/pic.jpg?X-Amz-Algorithm=AWS4", "branding-assets", "courses/thumbnails/pic.jpg")]
    [InlineData("https://minio.example.com/branding-assets/branding/logo.png", "branding-assets", "branding/logo.png")]
    [InlineData("courses/thumbnails/pic.jpg", "branding-assets", "courses/thumbnails/pic.jpg")]
    [InlineData("/branding-assets/courses/thumbnails/pic.jpg", "branding-assets", "courses/thumbnails/pic.jpg")]
    [InlineData("branding-assets/courses/thumbnails/pic.jpg", "branding-assets", "courses/thumbnails/pic.jpg")]
    [InlineData("custom-bucket/subfolder/file.pdf", "custom-bucket", "subfolder/file.pdf")]
    public void TryParseStoragePath_ShouldCorrectlyExtractBucketAndKey(string input, string expectedBucket, string expectedKey)
    {
        var result = ObjectStorageExtensions.TryParseStoragePath(input);

        Assert.NotNull(result);
        Assert.Equal(expectedBucket, result.Value.Bucket);
        Assert.Equal(expectedKey, result.Value.Key);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryParseStoragePath_ShouldReturnNull_WhenInputIsNullOrWhitespace(string? input)
    {
        var result = ObjectStorageExtensions.TryParseStoragePath(input);
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteObjectByUrlAsync_ShouldCallDeleteObjectAsync_WhenValidUrlProvided()
    {
        var storageService = Substitute.For<IObjectStorageService>();
        var url = "http://localhost:9000/branding-assets/courses/thumbnails/test.png";

        await storageService.DeleteObjectByUrlAsync(url);

        await storageService.Received(1).DeleteObjectAsync(
            "branding-assets",
            "courses/thumbnails/test.png",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteObjectByUrlAsync_ShouldNotCallDeleteObjectAsync_WhenUrlIsEmpty()
    {
        var storageService = Substitute.For<IObjectStorageService>();

        await storageService.DeleteObjectByUrlAsync(null);
        await storageService.DeleteObjectByUrlAsync("");

        await storageService.DidNotReceiveWithAnyArgs().DeleteObjectAsync(
            default!,
            default!,
            Arg.Any<CancellationToken>());
    }
}
