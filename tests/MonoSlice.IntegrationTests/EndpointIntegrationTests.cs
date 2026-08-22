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

    [Fact]
    public async Task PaymentsCheckoutAndWebhook_ShouldSucceed()
    {
        // 1. Register and Login to get access token
        var registerPayload = new
        {
            Email = "student_pay@example.com",
            UserName = "student_pay",
            Password = "Password123!",
            FullName = "Paying Student"
        };
        await _client.PostAsJsonAsync("/api/v1/auth/register", registerPayload);

        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            UserNameOrEmail = "student_pay@example.com",
            Password = "Password123!"
        });
        var loginData = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<MonoSlice.Modules.Users.Features.Login.LoginResponseDto>>();
        Assert.NotNull(loginData?.Data?.AccessToken);

        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginData.Data.AccessToken);

        // 2. Checkout
        var courseId = Guid.CreateVersion7();
        var checkoutPayload = new
        {
            CourseId = courseId,
            Amount = 150000m,
            Currency = "IDR"
        };

        var checkoutResponse = await _client.PostAsJsonAsync("/api/v1/payments/checkout", checkoutPayload);
        var checkoutRaw = await checkoutResponse.Content.ReadAsStringAsync();
        Assert.True(checkoutResponse.IsSuccessStatusCode, $"Checkout failed: {checkoutRaw}");

        var checkoutData = await checkoutResponse.Content.ReadFromJsonAsync<ApiResponse<MonoSlice.Modules.Orders.Features.CreateCheckout.CheckoutResponseDto>>();
        Assert.NotNull(checkoutData?.Data);
        var orderId = checkoutData.Data.OrderId;

        // 3. Query Order
        var getOrderResponse = await _client.GetAsync($"/api/v1/payments/orders/{orderId}");
        Assert.True(getOrderResponse.IsSuccessStatusCode);

        // 4. Webhook
        var webhookPayload = new
        {
            OrderId = orderId,
            ExternalPaymentReference = "GATEWAY-REF-1001",
            PaymentStatus = "PAID"
        };
        var webhookResponse = await _client.PostAsJsonAsync("/api/v1/payments/webhook", webhookPayload);
        var webhookRaw = await webhookResponse.Content.ReadAsStringAsync();
        Assert.True(webhookResponse.IsSuccessStatusCode, $"Webhook failed: {webhookRaw}");

        // 5. Query Order again to verify Paid status
        var verifyOrderResponse = await _client.GetAsync($"/api/v1/payments/orders/{orderId}");
        var orderData = await verifyOrderResponse.Content.ReadFromJsonAsync<ApiResponse<MonoSlice.Modules.Orders.Features.GetOrder.OrderResponseDto>>();
        Assert.NotNull(orderData?.Data);
        Assert.Equal("Paid", orderData.Data.Status);
    }
}
