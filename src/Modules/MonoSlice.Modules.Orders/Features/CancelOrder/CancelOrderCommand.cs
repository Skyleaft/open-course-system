using System.ComponentModel.DataAnnotations;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Orders.Features.CancelOrder;

public sealed record CancelOrderCommand(
    Guid OrderId,
    [MaxLength(200)] string? Reason = null) : ICommand<ApiResponse<string>>;
