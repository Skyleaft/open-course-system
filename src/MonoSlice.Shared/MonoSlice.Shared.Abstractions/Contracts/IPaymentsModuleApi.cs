namespace MonoSlice.Shared.Abstractions.Contracts;

public record OrderContractDto(
    Guid Id,
    Guid UserId,
    Guid CourseId,
    decimal Amount,
    string Currency,
    string Status,
    string? ExternalPaymentReference,
    DateTime CreatedAtUtc,
    DateTime? PaidAtUtc);

public interface IPaymentsModuleApi
{
    Task<OrderContractDto?> GetOrderByIdAsync(Guid orderId, CancellationToken ct = default);
    Task<bool> IsOrderPaidAsync(Guid orderId, CancellationToken ct = default);
    Task<bool> HasUserPurchasedCourseAsync(Guid userId, Guid courseId, CancellationToken ct = default);
}
