using CoreLib.Common;

namespace Web.Client.Services;

public class OnlineUserStateService
{
    private List<OnlineUser> _onlineUsers = new();
    public IReadOnlyList<OnlineUser> OnlineUsers => _onlineUsers;

    public event Action<List<OnlineUser>>? OnlineUsersChanged;

    public void UpdateOnlineUsers(List<OnlineUser> users)
    {
        _onlineUsers = users;
        OnlineUsersChanged?.Invoke(users);
    }
}