using System.Collections.Concurrent;
using CoreLib.Common;

namespace Web.Services.HubServices;

public class OnlineUserService
{
    private readonly ConcurrentDictionary<string, OnlineUser> _onlineUsers = new();

    public void AddUser(string connectionId, OnlineUser user)
    {
        _onlineUsers[connectionId] = user;
    }

    public void RemoveUser(string connectionId)
    {
        _onlineUsers.TryRemove(connectionId, out _);
    }

    public List<OnlineUser> GetAllUsers()
    {
        return _onlineUsers.Values.ToList();
    }

    public OnlineUser? GetUser(string connectionId)
    {
        _onlineUsers.TryGetValue(connectionId, out var user);
        return user;
    }
}