using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.Domain;
using MonoSlice.Shared.Infrastructure.Caching;
using MonoSlice.Shared.Infrastructure.Messaging;
using MonoSlice.Shared.Infrastructure.Storage;
using Xunit;

namespace MonoSlice.IntegrationTests;

public class TestAddress : ValueObject
{
    public string Street { get; }
    public string City { get; }
    public string ZipCode { get; }

    public TestAddress(string street, string city, string zipCode)
    {
        Street = street;
        City = city;
        ZipCode = zipCode;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
        yield return ZipCode;
    }
}

public class SharedInfrastructureTests
{
    [Fact]
    public void ValueObject_Equality_ShouldBeBasedOnComponents()
    {
        var addr1 = new TestAddress("Main St", "Jakarta", "10110");
        var addr2 = new TestAddress("Main St", "Jakarta", "10110");
        var addr3 = new TestAddress("Second St", "Jakarta", "10110");

        Assert.Equal(addr1, addr2);
        Assert.True(addr1 == addr2);
        Assert.False(addr1 == addr3);
        Assert.NotEqual(addr1, addr3);
        Assert.Equal(addr1.GetHashCode(), addr2.GetHashCode());
    }

    [Fact]
    public void ApiErrorResponse_Validation_ShouldStructureErrorsCorrectly()
    {
        var errors = new Dictionary<string, string[]>
        {
            ["Email"] = ["Email is required.", "Invalid email format."],
            ["Password"] = ["Password too short."]
        };

        var response = ApiErrorResponse.Validation(errors);

        Assert.False(response.Success);
        Assert.Equal("VALIDATION_ERROR", response.Code);
        Assert.Equal(400, response.StatusCode);
        Assert.Equal(3, response.Errors?.Count);
        Assert.Equal(2, response.ValidationErrors?.Count);
    }

    [Fact]
    public async Task InMemoryDistributedLock_ShouldAcquireAndRelease()
    {
        var lockService = new InMemoryDistributedLock();
        var resourceKey = "test-resource-" + Guid.NewGuid();

        await using (var handle = await lockService.AcquireLockAsync(resourceKey, TimeSpan.FromMinutes(1)))
        {
            Assert.NotNull(handle);
            Assert.True(handle.IsAcquired);

            // Second acquisition should timeout
            var secondHandle = await lockService.AcquireLockAsync(resourceKey, TimeSpan.FromMinutes(1), TimeSpan.FromMilliseconds(50));
            Assert.Null(secondHandle);
        }

        // After dispose, should be acquirable again
        await using var reacquiredHandle = await lockService.AcquireLockAsync(resourceKey, TimeSpan.FromMinutes(1));
        Assert.NotNull(reacquiredHandle);
        Assert.True(reacquiredHandle.IsAcquired);
    }

    [Fact]
    public async Task InMemoryEventStreamPublisher_ShouldPublishMessage()
    {
        var logger = NullLogger<InMemoryEventStreamPublisher>.Instance;
        var publisher = new InMemoryEventStreamPublisher(logger);

        var messageId = await publisher.PublishAsync("stream:test", new { Foo = "Bar" });

        Assert.NotNull(messageId);
        Assert.Contains("-0", messageId);
    }

    [Fact]
    public async Task MinioStorageService_GeneratePresignedUrls_ShouldGenerateValidUrls()
    {
        var settings = Options.Create(new StorageSettings
        {
            Endpoint = "localhost:9000",
            PublicEndpoint = "http://localhost:9000",
            AccessKey = "minioadmin",
            SecretKey = "minioadmin123",
            UseSSL = false
        });

        var logger = NullLogger<MinioObjectStorageService>.Instance;
        var storage = new MinioObjectStorageService(settings, logger);

        var uploadUrl = await storage.GeneratePresignedUploadUrlAsync("exam-snapshots", "snap-1.webp", TimeSpan.FromMinutes(2), "image/webp");
        var downloadUrl = await storage.GeneratePresignedDownloadUrlAsync("course-materials", "lecture-1.pdf", TimeSpan.FromMinutes(15));

        Assert.NotNull(uploadUrl);
        Assert.StartsWith("http://localhost:9000/exam-snapshots/snap-1.webp", uploadUrl);
        Assert.NotNull(downloadUrl);
        Assert.StartsWith("http://localhost:9000/course-materials/lecture-1.pdf", downloadUrl);
    }
}
