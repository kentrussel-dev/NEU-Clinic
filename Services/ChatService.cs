// Services/ChatService.cs
using WebApp.Data;
using WebApp.Models;
using Microsoft.EntityFrameworkCore;

public class ChatService
{
    private readonly AppDbContext _context;

    public ChatService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<PersonalMessage>> GetConversationAsync(string userId, string contactId, int skip = 0, int take = 50)
    {
        return await _context.PersonalMessages
            .Where(m => (m.SenderId == userId && m.ReceiverId == contactId) ||
                         (m.SenderId == contactId && m.ReceiverId == userId))
            .OrderByDescending(m => m.SentAt)
            .Skip(skip)
            .Take(take)
            .Include(m => m.Sender)
            .Include(m => m.Receiver)
            .OrderBy(m => m.SentAt)
            .ToListAsync();
    }

    public async Task<List<Users>> GetRecentContactsAsync(string userId)
    {
        var sentTo = await _context.PersonalMessages
            .Where(m => m.SenderId == userId)
            .OrderByDescending(m => m.SentAt)
            .Select(m => m.Receiver)
            .Distinct()
            .Take(5)
            .ToListAsync();

        var receivedFrom = await _context.PersonalMessages
            .Where(m => m.ReceiverId == userId)
            .OrderByDescending(m => m.SentAt)
            .Select(m => m.Sender)
            .Distinct()
            .Take(5)
            .ToListAsync();

        return sentTo.Union(receivedFrom)
            .GroupBy(u => u.Id)
            .Select(g => g.First())
            .OrderByDescending(u => _context.PersonalMessages
                .Where(m => (m.SenderId == userId && m.ReceiverId == u.Id) ||
                           (m.SenderId == u.Id && m.ReceiverId == userId))
                .Max(m => m.SentAt))
            .Take(10)
            .ToList();
    }

    public async Task<int> GetUnreadMessageCountAsync(string userId)
    {
        return await _context.PersonalMessages
            .CountAsync(m => m.ReceiverId == userId && !m.ReadAt.HasValue);
    }
}