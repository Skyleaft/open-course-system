namespace MonoSlice.Shared.Abstractions.Interfaces;

/// <summary>
/// Current authenticated user accessor.
/// </summary>
public interface ICurrentUser
{
    Guid? UserId { get; }
    string? Email { get; }
    string? UserName { get; }
    bool IsAuthenticated { get; }
    IReadOnlyList<string> Roles { get; }
    bool IsInRole(string role);
}
