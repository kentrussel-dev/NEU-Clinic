using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System;

namespace WebApp.Controllers
{
    [Authorize(Roles = "SuperAdmin,Admin,MedicalStaff")]
    public class UserHealthManagementController : Controller
    {
        private readonly UserManager<Users> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly AppDbContext _context;
        private readonly ILogger<UserHealthManagementController> _logger;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly EmailService _emailService; // Add EmailService

        public UserHealthManagementController(
            UserManager<Users> userManager,
            RoleManager<IdentityRole> roleManager,
            AppDbContext context,
            ILogger<UserHealthManagementController> logger,
            IWebHostEnvironment hostingEnvironment,
            EmailService emailService) // Inject EmailService
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _logger = logger;
            _hostingEnvironment = hostingEnvironment;
            _emailService = emailService; // Initialize EmailService
        }

        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            var userRoles = new Dictionary<string, List<string>>();
            var userHealthDetails = new Dictionary<string, HealthDetails>();
            var userSubmittedHealthDetails = new Dictionary<string, SubmittedHealthDetails>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userRoles[user.Id] = roles.ToList();

                var healthDetails = await _context.HealthDetails
                    .FirstOrDefaultAsync(h => h.UserId == user.Id);
                userHealthDetails[user.Id] = healthDetails ?? new HealthDetails { UserId = user.Id };

                // Fetch the latest SubmittedHealthDetails for the user
                var submittedHealthDetails = await _context.SubmittedHealthDetails
                    .Where(s => s.UserId == user.Id)
                    .OrderByDescending(s => s.SubmissionDate)
                    .FirstOrDefaultAsync();
                userSubmittedHealthDetails[user.Id] = submittedHealthDetails;
            }

            ViewBag.UserRoles = userRoles;
            ViewBag.Roles = _roleManager.Roles.ToList();
            ViewBag.UserHealthDetails = userHealthDetails;
            ViewBag.UserSubmittedHealthDetails = userSubmittedHealthDetails;

            return View(users);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateHealthDetails(
            string userId,
            string bloodType,
            string allergies,
            string medicalNotes,
            string emergencyContactName,
            string emergencyContactRelationship,
            string emergencyContactPhone,
            string chronicConditions,
            string medications,
            string immunizationHistory,
            string recentCheckups,
            string activityRestrictions,
            string dietaryRestrictions,
            string mentalHealthNotes,
            string healthAlerts, // JSON-serialized list of alerts
            IFormFile xrayFile,
            IFormFile medicalCertificate,
            IFormFile vaccinationRecord,
            IFormFile otherDocuments)
        {
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "User selection is required.";
                return RedirectToAction("Index");
            }

            var existingHealthDetails = await _context.HealthDetails
                .FirstOrDefaultAsync(h => h.UserId == userId);

            if (existingHealthDetails == null)
            {
                existingHealthDetails = new HealthDetails { UserId = userId };
                _context.HealthDetails.Add(existingHealthDetails);
            }

            // Update health details
            existingHealthDetails.BloodType = bloodType;
            existingHealthDetails.Allergies = allergies;
            existingHealthDetails.MedicalNotes = medicalNotes;
            existingHealthDetails.EmergencyContactName = emergencyContactName;
            existingHealthDetails.EmergencyContactRelationship = emergencyContactRelationship;
            existingHealthDetails.EmergencyContactPhone = emergencyContactPhone;
            existingHealthDetails.ChronicConditions = chronicConditions;
            existingHealthDetails.Medications = medications;
            existingHealthDetails.ImmunizationHistory = immunizationHistory;
            existingHealthDetails.RecentCheckups = recentCheckups;
            existingHealthDetails.ActivityRestrictions = activityRestrictions;
            existingHealthDetails.DietaryRestrictions = dietaryRestrictions;
            existingHealthDetails.MentalHealthNotes = mentalHealthNotes;

            // Update Health Alerts
            existingHealthDetails.HealthAlerts = healthAlerts;

            // Handle file uploads (if any)
            if (xrayFile != null && xrayFile.Length > 0)
            {
                existingHealthDetails.XRayFileUrl = await SaveFile(xrayFile);
            }
            if (medicalCertificate != null && medicalCertificate.Length > 0)
            {
                existingHealthDetails.MedicalCertificateUrl = await SaveFile(medicalCertificate);
            }
            if (vaccinationRecord != null && vaccinationRecord.Length > 0)
            {
                existingHealthDetails.VaccinationRecordUrl = await SaveFile(vaccinationRecord);
            }
            if (otherDocuments != null && otherDocuments.Length > 0)
            {
                existingHealthDetails.OtherDocumentsUrl = await SaveFile(otherDocuments);
            }

            try
            {
                await _context.SaveChangesAsync();

                // Send notification to the user
                await SendSystemNotification(userId, "Your health details have been updated by the medical staff.");

                // Send an email to the user
                var recipient = await _userManager.FindByIdAsync(userId);
                if (recipient != null)
                {
                    var subject = "Health Details Updated";
                    var emailMessage = $"Your health details have been updated by the medical staff.<br><br>Details:<br>{medicalNotes}";
                    await _emailService.SendEmailAsync(recipient.Email, subject, emailMessage);
                }

                TempData["SuccessMessage"] = "Health details updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error saving health details: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while updating health details.";
            }

            return RedirectToAction("Index");
        }

        private async Task<string> SaveFile(IFormFile file)
        {
            var uploadsFolder = Path.Combine(_hostingEnvironment.WebRootPath, "uploads");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            return "/uploads/" + uniqueFileName;
        }

        private async Task SendSystemNotification(string userId, string message)
        {
            var notification = new Notification
            {
                UserId = userId,
                SenderEmail = "System", // Set the sender as "System"
                Message = message,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }
    }
}