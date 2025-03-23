using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Threading.Tasks;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.Controllers
{
    public class NotificationsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Users> _userManager;
        private readonly ILogger<NotificationsController> _logger;

        // Only one constructor
        public NotificationsController(
            AppDbContext context,
            UserManager<Users> userManager,
            ILogger<NotificationsController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: Notifications/Details/5
        public async Task<IActionResult> Details(int id)
        {
            _logger.LogInformation("Fetching notification with ID: {Id}", id);

            var notification = await _context.Notifications
                .Include(n => n.User) // Include the user details
                .FirstOrDefaultAsync(n => n.Id == id);

            if (notification == null)
            {
                _logger.LogWarning("Notification with ID {Id} not found", id);
                return NotFound();
            }

            // Mark the notification as read
            if (!notification.IsRead)
            {
                notification.IsRead = true;
                _context.Notifications.Update(notification);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Notification with ID {Id} marked as read", id);
            }

            return View(notification);
        }

        // POST: Notifications/MarkAsRead/5
        [HttpPost("Notifications/MarkAsRead/{id}")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }
            return Ok();
        }

        // POST: Notifications/MarkAllAsRead
        [HttpPost("Notifications/MarkAllAsRead")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = _userManager.GetUserId(User); // Get the current user's ID
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(); // User is not authenticated
            }

            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
            }

            await _context.SaveChangesAsync();
            return Ok();
        }

        // GET: Notifications/GetNotificationsForUser
        public async Task<IActionResult> GetNotificationsForUser()
        {
            var userId = _userManager.GetUserId(User); // Get the current user's ID
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(); // User is not authenticated
            }

            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            return Json(notifications);
        }
    }
}