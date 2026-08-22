using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Assessments.Features.Admin.RedriveDeadLetter;

public sealed record RedriveDeadLetterCommand(Guid Id) : ICommand<ApiResponse>;
