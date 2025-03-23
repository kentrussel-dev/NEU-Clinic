using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;
using System.Threading.Tasks;

public class NotificationService
{
    private readonly AppDbContext _context;

    public NotificationService(AppDbContext context)
    {
        _context = context;
    }

    public async Task NotifyUserAsync(string userId, string senderEmail, string message)
    {
        var notification = new Notification
        {
            UserId = userId,
            SenderEmail = senderEmail,
            Message = message,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> HasUnreadNotificationsAsync(string userId)
    {
        return await _context.Notifications
            .AnyAsync(n => n.UserId == userId && !n.IsRead);
    }

    public async Task<List<Notification>> GetNotificationsForUserAsync(string userId)
    {
        return await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }
}