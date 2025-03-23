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
    private readonly EmailService _emailService;
    private readonly IConfiguration _configuration;

    public SendNotificationController(
        UserManager<Users> userManager,
        AppDbContext context,
        EmailService emailService,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _context = context;
        _emailService = emailService;
        _configuration = configuration;
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
        string senderEmail;

        if (sender == null)
        {
            // If sender is not found, use the SuperAdmin email from appsettings
            senderEmail = _configuration["SuperAdmin:Email"];
        }
        else
        {
            senderEmail = sender.Email;
        }

        // Get the recipient user
        var recipient = await _userManager.FindByIdAsync(userId);
        if (recipient == null)
        {
            TempData["ErrorMessage"] = "Recipient not found.";
            return RedirectToAction("Index");
        }

        // Create the notification with the sender's email
        var notification = new Notification
        {
            UserId = userId,
            SenderEmail = senderEmail, // Use the sender's email or SuperAdmin email
            Message = message,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // Send an email to the recipient
        try
        {
            var subject = "New Notification";
            var emailMessage = $"You have received a new notification from {senderEmail}:<br><br>{message}";
            await _emailService.SendEmailAsync(recipient.Email, subject, emailMessage);

            TempData["SuccessMessage"] = "Notification sent successfully!";
        }
        catch (Exception ex)
        {
            // Log the full exception details for debugging
            var errorMessage = $"Notification saved, but email could not be sent: {ex.Message}";

            if (ex.InnerException != null)
            {
                errorMessage += $"\nInner Exception: {ex.InnerException.Message}";
            }

            errorMessage += $"\nStack Trace: {ex.StackTrace}";

            // Store the full error message in TempData
            TempData["ErrorMessage"] = errorMessage;
        }

        return RedirectToAction("Index");
    }
}