using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using WebApp.Data;
using WebApp.Models;
using Microsoft.EntityFrameworkCore;

public class ChatHub : Hub
{
    private static readonly ConcurrentDictionary<string, string> _userConnections = new ConcurrentDictionary<string, string>();
    private readonly AppDbContext _context;

    public ChatHub(AppDbContext context)
    {
        _context = context;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        if (userId != null)
        {
            _userConnections.AddOrUpdate(userId, Context.ConnectionId, (key, oldValue) => Context.ConnectionId);
            await Clients.All.SendAsync("UserOnlineStatusChanged", userId, true);
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        var userId = Context.UserIdentifier;
        if (userId != null && _userConnections.TryRemove(userId, out _))
        {
            await Clients.All.SendAsync("UserOnlineStatusChanged", userId, false);
        }
        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendPrivateMessage(string receiverId, string message)
    {
        var senderId = Context.UserIdentifier;
        if (string.IsNullOrEmpty(senderId)) return;

        var messageEntity = new PersonalMessage
        {
            SenderId = senderId,
            ReceiverId = receiverId,
            Content = message,
            SentAt = DateTime.Now
        };

        _context.PersonalMessages.Add(messageEntity);
        await _context.SaveChangesAsync();

        var sender = await _context.Users.FindAsync(senderId);

        var messageData = new
        {
            Id = messageEntity.Id,
            SenderId = senderId,
            SenderName = sender.FullName ?? sender.UserName,
            SenderProfilePic = sender.ProfilePictureUrl ?? "/images/default-profile.png",
            Content = message,
            SentAt = messageEntity.SentAt,
            ReadAt = messageEntity.ReadAt
        };

        // Send to receiver if online
        if (_userConnections.TryGetValue(receiverId, out var receiverConnectionId))
        {
            await Clients.Client(receiverConnectionId).SendAsync("ReceivePrivateMessage", messageData);
        }

        // Send back to sender for their own UI
        await Clients.Caller.SendAsync("ReceivePrivateMessage", messageData);
    }

    public async Task MarkMessagesAsRead(string contactId)
    {
        var userId = Context.UserIdentifier;
        if (string.IsNullOrEmpty(userId)) return;

        var unreadMessages = await _context.PersonalMessages
            .Where(m => m.SenderId == contactId && m.ReceiverId == userId && !m.ReadAt.HasValue)
            .ToListAsync();

        foreach (var message in unreadMessages)
        {
            message.ReadAt = DateTime.Now;
        }

        await _context.SaveChangesAsync();

        // Notify the sender that their messages were read
        if (_userConnections.TryGetValue(contactId, out var senderConnectionId))
        {
            await Clients.Client(senderConnectionId).SendAsync("MessagesRead", userId);
        }
    }

    public async Task<IEnumerable<string>> GetOnlineUsers()
    {
        return _userConnections.Keys.ToList();
    }
}