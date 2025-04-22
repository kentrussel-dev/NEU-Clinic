using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.Controllers
{
    public class EmailSenderController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Users> _userManager;
        private readonly EmailService _emailService;
        private readonly NotificationService _notificationService;

        public EmailSenderController(
            AppDbContext context,
            UserManager<Users> userManager,
            EmailService emailService,
            NotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
            _notificationService = notificationService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers(string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
                return Json(new List<object>());

            var users = await _userManager.Users
                .Where(u => (u.UserName.Contains(searchTerm) ||
                            u.Email.Contains(searchTerm) ||
                            u.FullName.Contains(searchTerm)) &&
                            u.Email != null)
                .Select(u => new
                {
                    id = u.Id,
                    userName = u.UserName,
                    email = u.Email,
                    fullName = u.FullName,
                    profilePictureUrl = u.ProfilePictureUrl
                })
                .Take(10)
                .ToListAsync();

            return Json(users);
        }

        [HttpPost]
        public async Task<IActionResult> SendEmail([FromForm] EmailViewModel model)
        {
            try
            {
                if (ModelState.IsValid && model.UserIds != null && model.UserIds.Count > 0)
                {
                    // Build email content
                    string emailBody = model.Message;

                    // Handle attachments
                    List<string> attachmentPaths = new List<string>();
                    if (model.Attachments != null && model.Attachments.Count > 0)
                    {
                        foreach (var file in model.Attachments)
                        {
                            if (file.Length > 0)
                            {
                                // Check file type (exclude videos)
                                string extension = Path.GetExtension(file.FileName).ToLower();
                                if (extension == ".mp4" || extension == ".avi" || extension == ".mov" ||
                                    extension == ".wmv" || extension == ".flv" || extension == ".mkv")
                                {
                                    return Json(new { success = false, message = "Video files are not allowed." });
                                }

                                // Check file size (limit to 10MB)
                                if (file.Length > 10 * 1024 * 1024)
                                {
                                    return Json(new { success = false, message = "File size should not exceed 10MB." });
                                }

                                // Save file to temp directory
                                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                                string filePath = Path.Combine(Path.GetTempPath(), fileName);
                                using (var stream = new FileStream(filePath, FileMode.Create))
                                {
                                    await file.CopyToAsync(stream);
                                }
                                attachmentPaths.Add(filePath);
                            }
                        }
                    }

                    int successCount = 0;
                    List<string> failedEmails = new List<string>();

                    // Process each selected user
                    foreach (var userId in model.UserIds)
                    {
                        // Get the user to send email to
                        var user = await _userManager.FindByIdAsync(userId);
                        if (user == null || string.IsNullOrEmpty(user.Email))
                        {
                            failedEmails.Add(userId);
                            continue;
                        }

                        // Send email with attachments
                        await _emailService.SendEmailWithAttachmentsAsync(user.Email, model.Subject, emailBody, attachmentPaths);

                        // Create notification
                        await _notificationService.NotifyUserAsync(user.Id, User.Identity.Name,
                            $"You have received an email: {model.Subject}");

                        successCount++;
                    }

                    // Build response message
                    string message;
                    if (successCount == model.UserIds.Count)
                    {
                        message = $"Email sent successfully to {successCount} recipient{(successCount != 1 ? "s" : "")}.";
                        return Json(new { success = true, message = message });
                    }
                    else if (successCount > 0)
                    {
                        message = $"Email sent to {successCount} recipient{(successCount != 1 ? "s" : "")}, but failed for {failedEmails.Count} recipient{(failedEmails.Count != 1 ? "s" : "")}.";
                        return Json(new { success = true, message = message, partialSuccess = true });
                    }
                    else
                    {
                        message = "Failed to send emails. No valid recipients found.";
                        return Json(new { success = false, message = message });
                    }
                }

                return Json(new { success = false, message = "Invalid form data or no recipients selected." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error sending email: {ex.Message}" });
            }
        }
    }
}