using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;
using System.Security.Claims;
using System.Linq;
using System.Threading.Tasks;

[Authorize]
public class ChatController : Controller
{
    private readonly UserManager<Users> _userManager;
    private readonly AppDbContext _context;

    public ChatController(UserManager<Users> userManager, AppDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetConversation(string contactId, int skip = 0, int take = 50)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return BadRequest();

        var messages = await _context.PersonalMessages
            .Where(m => (m.SenderId == userId && m.ReceiverId == contactId) ||
                       (m.SenderId == contactId && m.ReceiverId == userId))
            .OrderByDescending(m => m.SentAt)
            .Skip(skip)
            .Take(take)
            .Include(m => m.Sender)
            .Include(m => m.Receiver)
            .OrderBy(m => m.SentAt)
            .ToListAsync();

        return Ok(messages.Select(m => new
        {
            Id = m.Id,
            SenderId = m.SenderId,
            SenderName = m.Sender.FullName ?? m.Sender.UserName,
            SenderProfilePic = m.Sender.ProfilePictureUrl ?? "/images/default-profile.png",
            ReceiverId = m.ReceiverId,
            Content = m.Content,
            SentAt = m.SentAt,
            ReadAt = m.ReadAt
        }));
    }

    [HttpGet]
    public async Task<IActionResult> GetRecentContacts()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return BadRequest();

        // Get all users except current user
        var allUsers = await _userManager.Users
            .Where(u => u.Id != userId)
            .ToListAsync();

        // Get recent contacts (users you've messaged or who messaged you)
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

        var recentContacts = sentTo.Union(receivedFrom)
            .GroupBy(u => u.Id)
            .Select(g => g.First())
            .OrderByDescending(u => _context.PersonalMessages
                .Where(m => (m.SenderId == userId && m.ReceiverId == u.Id) ||
                           (m.SenderId == u.Id && m.ReceiverId == userId))
                .Max(m => m.SentAt))
            .Take(10)
            .ToList();

        // If no recent contacts, return all users
        var contacts = recentContacts.Any() ? recentContacts : allUsers.Take(10).ToList();

        return Ok(contacts.Select(c => new
        {
            Id = c.Id,
            Name = c.FullName ?? c.UserName,
            ProfilePic = c.ProfilePictureUrl ?? "/images/default-profile.png",
            IsOnline = false // You would implement online status tracking
        }));
    }

    [HttpGet]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return BadRequest();

        var count = await _context.PersonalMessages
            .CountAsync(m => m.ReceiverId == userId && !m.ReadAt.HasValue);

        return Ok(new { count });
    }
}