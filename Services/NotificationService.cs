using WebApp.Models;
using WebApp.Data;
using Microsoft.EntityFrameworkCore;

public class NotificationService
{
    private readonly AppDbContext _context;

    public NotificationService(AppDbContext context)
    {
        _context = context;
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
