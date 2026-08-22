using System.Text.Json.Serialization;
using MonoSlice.Modules.Catalog.Features.CreateProduct;
using MonoSlice.Modules.Catalog.Features.DeleteProduct;
using MonoSlice.Modules.Catalog.Features.GetProduct;
using MonoSlice.Modules.Catalog.Features.ListProducts;
using MonoSlice.Modules.Catalog.Features.UpdateProduct;
using MonoSlice.Modules.Orders.Domain;
using MonoSlice.Modules.Orders.Features.CancelOrder;
using MonoSlice.Modules.Orders.Features.CreateOrder;
using MonoSlice.Modules.Orders.Features.GetOrder;
using MonoSlice.Modules.Orders.Features.GetOrderAnalytics;
using MonoSlice.Modules.Orders.Features.ListOrders;
using MonoSlice.Modules.Orders.Features.ProcessOrderAsync;
using MonoSlice.Modules.Users.Features.AssignRole;
using MonoSlice.Modules.Users.Features.GetProfile;
using MonoSlice.Modules.Users.Features.Login;
using MonoSlice.Modules.Users.Features.RefreshToken;
using MonoSlice.Modules.Users.Features.Register;
using MonoSlice.Shared.Abstractions.Common;

namespace MonoSlice.Host;

[JsonSerializable(typeof(ApiResponse))]
[JsonSerializable(typeof(ApiResponse<string>))]
[JsonSerializable(typeof(ApiResponse<UserResponseDto>))]
[JsonSerializable(typeof(ApiResponse<LoginResponseDto>))]
[JsonSerializable(typeof(ApiResponse<RefreshTokenResponseDto>))]
[JsonSerializable(typeof(ApiResponse<ProductDto>))]
[JsonSerializable(typeof(ApiResponse<PaginatedList<ProductDto>>))]
[JsonSerializable(typeof(PaginatedList<ProductDto>))]
[JsonSerializable(typeof(ApiResponse<OrderDto>))]
[JsonSerializable(typeof(ApiResponse<PaginatedList<OrderDto>>))]
[JsonSerializable(typeof(PaginatedList<OrderDto>))]
[JsonSerializable(typeof(RegisterCommand))]
[JsonSerializable(typeof(UserResponseDto))]
[JsonSerializable(typeof(LoginCommand))]
[JsonSerializable(typeof(LoginResponseDto))]
[JsonSerializable(typeof(UserInfoDto))]
[JsonSerializable(typeof(RefreshTokenCommand))]
[JsonSerializable(typeof(RefreshTokenResponseDto))]
[JsonSerializable(typeof(AssignRoleCommand))]
[JsonSerializable(typeof(GetProfileQuery))]
[JsonSerializable(typeof(CreateProductCommand))]
[JsonSerializable(typeof(ProductDto))]
[JsonSerializable(typeof(UpdateProductCommand))]
[JsonSerializable(typeof(DeleteProductCommand))]
[JsonSerializable(typeof(GetProductQuery))]
[JsonSerializable(typeof(ListProductsQuery))]
[JsonSerializable(typeof(CreateOrderCommand))]
[JsonSerializable(typeof(CreateOrderItemDto))]
[JsonSerializable(typeof(List<CreateOrderItemDto>))]
[JsonSerializable(typeof(OrderDto))]
[JsonSerializable(typeof(OrderItemDto))]
[JsonSerializable(typeof(List<OrderItemDto>))]
[JsonSerializable(typeof(IReadOnlyList<OrderItemDto>))]
[JsonSerializable(typeof(OrderStatus))]
[JsonSerializable(typeof(GetOrderQuery))]
[JsonSerializable(typeof(ListOrdersQuery))]
[JsonSerializable(typeof(ProcessOrderAsyncCommand))]
[JsonSerializable(typeof(CancelOrderCommand))]
[JsonSerializable(typeof(CancelOrderRequest))]
[JsonSerializable(typeof(GetOrderAnalyticsQuery))]
[JsonSerializable(typeof(ApiResponse<OrderAnalyticsDto>))]
[JsonSerializable(typeof(OrderAnalyticsDto))]
[JsonSerializable(typeof(OrderStatusBreakdownDto))]
[JsonSerializable(typeof(List<OrderStatusBreakdownDto>))]
[JsonSerializable(typeof(IReadOnlyList<OrderStatusBreakdownDto>))]
[JsonSerializable(typeof(TopPurchasedProductDto))]
[JsonSerializable(typeof(List<TopPurchasedProductDto>))]
[JsonSerializable(typeof(IReadOnlyList<TopPurchasedProductDto>))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(int?))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(bool?))]
[JsonSerializable(typeof(decimal))]
[JsonSerializable(typeof(decimal?))]
[JsonSerializable(typeof(Guid))]
[JsonSerializable(typeof(Guid?))]
[JsonSerializable(typeof(DateTime))]
[JsonSerializable(typeof(DateTime?))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(IReadOnlyList<string>))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
public partial class AppJsonSerializerContext : JsonSerializerContext
{
}
