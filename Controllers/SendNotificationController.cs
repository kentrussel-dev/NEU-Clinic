using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using WebApp.Models;
using WebApp.Data;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public class SendNotificationController : Controller
{
    private readonly UserManager<Users> _userManager;
    private readonly AppDbContext _context;

    public SendNotificationController(UserManager<Users> userManager, AppDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    // GET: SendNotification/Index
    public async Task<IActionResult> Index()
    {
        var users = await _userManager.Users.ToListAsync();
        return View(users);
    }

    // POST: SendNotification/Send
    [HttpPost]
    public async Task<IActionResult> Send(string message, string userId)
    {
        if (string.IsNullOrEmpty(message))
        {
            TempData["ErrorMessage"] = "Message cannot be empty.";
            return RedirectToAction("Index");
        }

        // Get the current user (sender)
        var sender = await _userManager.GetUserAsync(User);
        if (sender == null)
        {
            TempData["ErrorMessage"] = "Sender not found.";
            return RedirectToAction("Index");
        }

        // Create the notification with the sender's email
        var notification = new Notification
        {
            UserId = userId,
            SenderEmail = sender.Email, // Add the sender's email
            Message = message,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Notification sent successfully!";
        return RedirectToAction("Index");
    }
}