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
        private readonly ILogger<EmailSenderController> _logger;

        public EmailSenderController(
            AppDbContext context,
            UserManager<Users> userManager,
            EmailService emailService,
            NotificationService notificationService,
            ILogger<EmailSenderController> logger)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
            _notificationService = notificationService;
            _logger = logger;
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
        [HttpGet]
        public IActionResult GetIncompleteStudentCount()
        {
            var count = _context.Users
                .Join(_context.UserRoles,
                    user => user.Id,
                    userRole => userRole.UserId,
                    (user, userRole) => new { User = user, UserRole = userRole })
                .Join(_context.Roles,
                    ur => ur.UserRole.RoleId,
                    role => role.Id,
                    (ur, role) => new { ur.User, Role = role })
                .Where(x => x.Role.Name == "Student")
                .Select(x => x.User)
                .Count(u => u.HealthDetails != null &&
                           (!string.IsNullOrEmpty(u.HealthDetails.EmergencyContactName) ||
                            !string.IsNullOrEmpty(u.HealthDetails.XRayFileUrl) ||
                            !string.IsNullOrEmpty(u.HealthDetails.MedicalCertificateUrl) ||
                            !string.IsNullOrEmpty(u.HealthDetails.VaccinationRecordUrl)) &&
                           (u.HealthDetails.LastReminderSent == null ||
                            u.HealthDetails.LastReminderSent < DateTime.Now.AddMinutes(-1)));

            return Json(count);
        }

        [HttpPost]
        public async Task<IActionResult> SendReminderBatch([FromForm] int batchSize)
        {
            var studentsToNotify = await _context.Users
                .Join(_context.UserRoles,
                    user => user.Id,
                    userRole => userRole.UserId,
                    (user, userRole) => new { User = user, UserRole = userRole })
                .Join(_context.Roles,
                    ur => ur.UserRole.RoleId,
                    role => role.Id,
                    (ur, role) => new { ur.User, Role = role })
                .Where(x => x.Role.Name == "Student")
                .Select(x => x.User)
                .Include(u => u.HealthDetails)
                .Where(u => u.HealthDetails != null &&
                           !string.IsNullOrEmpty(u.Email) &&
                           (!string.IsNullOrEmpty(u.HealthDetails.EmergencyContactName) ||
                            !string.IsNullOrEmpty(u.HealthDetails.XRayFileUrl) ||
                            !string.IsNullOrEmpty(u.HealthDetails.MedicalCertificateUrl) ||
                            !string.IsNullOrEmpty(u.HealthDetails.VaccinationRecordUrl)))
                // Only send to students who haven't been reminded in the last hour
                .Where(u => u.HealthDetails.LastReminderSent == null ||
                           u.HealthDetails.LastReminderSent < DateTime.Now.AddMinutes(-1))
                .Take(batchSize)
                .ToListAsync();

            int sentCount = 0;
            int failedCount = 0;

            foreach (var student in studentsToNotify)
            {
                try
                {
                    var healthDetails = student.HealthDetails;
                    bool hasEmergency = !string.IsNullOrEmpty(healthDetails.EmergencyContactName) &&
                                      !string.IsNullOrEmpty(healthDetails.EmergencyContactPhone);
                    bool hasXRay = !string.IsNullOrEmpty(healthDetails.XRayFileUrl);
                    bool hasMedicalCert = !string.IsNullOrEmpty(healthDetails.MedicalCertificateUrl);
                    bool hasVaccination = !string.IsNullOrEmpty(healthDetails.VaccinationRecordUrl);

                    var emailBody = $@"
                <h3>Dear {student.FullName ?? student.UserName},</h3>
                <p>Our records show that you haven't completed all required health documents.</p>
                <p>Please submit the following missing documents as soon as possible:</p>
                <ul>
                    {(hasEmergency ? "" : "<li>Emergency Contact Information</li>")}
                    {(hasXRay ? "" : "<li>X-Ray Results</li>")}
                    {(hasMedicalCert ? "" : "<li>Medical Certificate</li>")}
                    {(hasVaccination ? "" : "<li>Vaccination Record</li>")}
                </ul>
                <p>Thank you for your cooperation.</p>
                <p>Sincerely,<br>Health Services</p>
            ";

                    await _emailService.SendEmailAsync(
                        student.Email,
                        "Reminder: Incomplete Health Requirements",
                        emailBody);

                    await _notificationService.NotifyUserAsync(
                        student.Id,
                        User.Identity.Name,
                        "Reminder: You have incomplete health requirements");

                    // Update the LastReminderSent timestamp
                    healthDetails.LastReminderSent = DateTime.Now;
                    sentCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to send reminder to {student.Email}");
                    failedCount++;
                }
            }

            // Save all changes at once
            await _context.SaveChangesAsync();

            return Json(new { sentCount, failedCount });
        }

        [HttpGet]
        public IActionResult GetExpiringDocumentCount()
        {
            var count = _context.Users
                .Join(_context.UserRoles,
                    user => user.Id,
                    userRole => userRole.UserId,
                    (user, userRole) => new { User = user, UserRole = userRole })
                .Join(_context.Roles,
                    ur => ur.UserRole.RoleId,
                    role => role.Id,
                    (ur, role) => new { ur.User, Role = role })
                .Where(x => x.Role.Name == "Student")
                .Select(x => x.User)
                .Count(u => u.HealthDetails != null &&
                           (u.HealthDetails.MedicalCertificateExpiryDate.HasValue ||
                            u.HealthDetails.VaccinationRecordExpiryDate.HasValue ||
                            u.HealthDetails.XRayExpiryDate.HasValue));

            return Json(count);
        }

        [HttpPost]
        public async Task<IActionResult> SendExpiryReminderBatch([FromForm] int batchSize)
        {
            var studentsToNotify = await _context.Users
                .Join(_context.UserRoles,
                    user => user.Id,
                    userRole => userRole.UserId,
                    (user, userRole) => new { User = user, UserRole = userRole })
                .Join(_context.Roles,
                    ur => ur.UserRole.RoleId,
                    role => role.Id,
                    (ur, role) => new { ur.User, Role = role })
                .Where(x => x.Role.Name == "Student")
                .Select(x => x.User)
                .Include(u => u.HealthDetails)
                .Where(u => u.HealthDetails != null &&
                           (u.HealthDetails.MedicalCertificateExpiryDate.HasValue ||
                            u.HealthDetails.VaccinationRecordExpiryDate.HasValue ||
                            u.HealthDetails.XRayExpiryDate.HasValue))
                // Only send to students who haven't received an expiry reminder today
                .Where(u => u.HealthDetails.LastExpiryReminderSent == null ||
                           u.HealthDetails.LastExpiryReminderSent.Value.Date < DateTime.Today)
                .Take(batchSize)
                .ToListAsync();

            int sentCount = 0;
            int failedCount = 0;

            foreach (var student in studentsToNotify)
            {
                try
                {
                    var healthDetails = student.HealthDetails;
                    var expiringDocuments = new List<string>();

                    if (healthDetails.MedicalCertificateExpiryDate.HasValue)
                    {
                        expiringDocuments.Add($"Medical Certificate (Expires: {healthDetails.MedicalCertificateExpiryDate.Value.ToShortDateString()})");
                    }
                    if (healthDetails.VaccinationRecordExpiryDate.HasValue)
                    {
                        expiringDocuments.Add($"Vaccination Record (Expires: {healthDetails.VaccinationRecordExpiryDate.Value.ToShortDateString()})");
                    }
                    if (healthDetails.XRayExpiryDate.HasValue)
                    {
                        expiringDocuments.Add($"X-Ray Results (Expires: {healthDetails.XRayExpiryDate.Value.ToShortDateString()})");
                    }

                    var emailBody = $@"
                <h3>Dear {student.FullName ?? student.UserName},</h3>
                <p>This is a reminder about upcoming document expirations in your health records:</p>
                <ul>
                    {string.Join("", expiringDocuments.Select(d => $"<li>{d}</li>"))}
                </ul>
                <p>Please renew these documents before they expire to maintain your complete health record status.</p>
                <p>Thank you for your attention to this matter.</p>
                <p>Sincerely,<br>Health Services</p>
            ";

                    await _emailService.SendEmailAsync(
                        student.Email,
                        "Reminder: Upcoming Document Expirations",
                        emailBody);

                    // Update the LastExpiryReminderSent timestamp
                    healthDetails.LastExpiryReminderSent = DateTime.Now;
                    sentCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to send expiry reminder to {student.Email}");
                    failedCount++;
                }
            }

            await _context.SaveChangesAsync();
            return Json(new { sentCount, failedCount });
        }

        public class EmailReminderRequest
        {
            public string CustomMessage { get; set; } // Optional custom message to include
        }
    }
}