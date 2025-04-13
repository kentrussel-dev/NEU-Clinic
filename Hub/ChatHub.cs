using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using System.Collections.Generic;

public class ChatHub : Hub
{
    private static readonly Dictionary<string, string> userGroups = new Dictionary<string, string>();
    private static readonly Dictionary<string, string> connectionUsers = new Dictionary<string, string>();

    public async Task JoinGroup(string groupName, string userName)
    {
        // Store the user name for this connection
        connectionUsers[Context.ConnectionId] = userName;

        // Remove from previous group if any
        if (userGroups.TryGetValue(Context.ConnectionId, out string previousGroup))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, previousGroup);
        }

        // Add to new group
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        userGroups[Context.ConnectionId] = groupName;

        // Notify group about new user
        await Clients.Group(groupName).SendAsync("UserJoined", userName);
    }

    public async Task SendMessageToGroup(string groupName, string user, string message)
    {
        await Clients.Group(groupName).SendAsync("ReceiveMessage", user, message, DateTime.Now);
    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        if (userGroups.TryGetValue(Context.ConnectionId, out string groupName))
        {
            userGroups.Remove(Context.ConnectionId);
        }
        if (connectionUsers.TryGetValue(Context.ConnectionId, out string userName))
        {
            connectionUsers.Remove(Context.ConnectionId);
        }
        await base.OnDisconnectedAsync(exception);
    }
}