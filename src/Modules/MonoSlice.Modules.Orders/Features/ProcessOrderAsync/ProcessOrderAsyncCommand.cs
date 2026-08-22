using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Orders.Features.ProcessOrderAsync;

public sealed record ProcessOrderAsyncCommand(Guid OrderId) : ICommand<ApiResponse<string>>;
