using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using MonoSlice.Shared.Abstractions.Common;
using Xunit;

namespace MonoSlice.IntegrationTests;

public class MonoSliceApplicationFactory : WebApplicationFactory<Program>
{
    static MonoSliceApplicationFactory()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__UsersDb", "InMemory:IntegrationTestUsersDb");
        Environment.SetEnvironmentVariable("ConnectionStrings__CatalogDb", "InMemory:IntegrationTestCatalogDb");
        Environment.SetEnvironmentVariable("ConnectionStrings__OrdersDb", "InMemory:IntegrationTestOrdersDb");
        Environment.SetEnvironmentVariable("Cache__Provider", "Memory");
        Environment.SetEnvironmentVariable("Messaging__Provider", "RabbitMQ");
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
    }
}

public class EndpointIntegrationTests : IClassFixture<MonoSliceApplicationFactory>
{
    private readonly HttpClient _client;

    public EndpointIntegrationTests(MonoSliceApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthCheck_ShouldReturnHealthy()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetProducts_ShouldReturnOkWithPaginatedList()
    {
        // Act
        var response = await _client.GetAsync("/api/catalog/products");
        var rawContent = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.True(response.IsSuccessStatusCode, $"Endpoint returned {response.StatusCode}: {rawContent}");
        var content = await response.Content.ReadFromJsonAsync<ApiResponse<PaginatedList<object>>>();
        Assert.NotNull(content);
        Assert.True(content.Success);
    }

    [Fact]
    public async Task AuthRegisterAndLogin_ShouldSucceed()
    {
        var registerPayload = new
        {
            Email = "student1@example.com",
            UserName = "student1",
            Password = "Password123!",
            FullName = "Student One"
        };

        var regResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", registerPayload);
        var regRaw = await regResponse.Content.ReadAsStringAsync();
        Assert.True(regResponse.IsSuccessStatusCode, $"Register failed: {regRaw}");

        var loginPayload = new
        {
            UserNameOrEmail = "student1@example.com",
            Password = "Password123!"
        };

        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", loginPayload);
        var loginRaw = await loginResponse.Content.ReadAsStringAsync();
        Assert.True(loginResponse.IsSuccessStatusCode, $"Login failed: {loginRaw}");
    }
}
