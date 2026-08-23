using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Catalog.Features.DeleteSection;

public sealed record DeleteSectionCommand(Guid SectionId) : ICommand<ApiResponse>;
