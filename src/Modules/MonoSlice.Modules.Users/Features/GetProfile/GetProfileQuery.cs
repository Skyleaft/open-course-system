using MonoSlice.Modules.Users.Features.Register;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Users.Features.GetProfile;

public sealed record GetProfileQuery : IQuery<ApiResponse<UserResponseDto>>;
