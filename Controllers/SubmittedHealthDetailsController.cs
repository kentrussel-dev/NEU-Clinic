using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;
using WebApp.ViewModels;
using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WebApp.Controllers
{
    public class SubmittedHealthDetailsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Users> _userManager;
        private readonly EmailService _emailService;

        public SubmittedHealthDetailsController(AppDbContext context, UserManager<Users> userManager, EmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
        }

        public async Task<IActionResult> Index()
        {
            var records = await _context.SubmittedHealthDetails
                .Include(s => s.User) // Include the User
                .ThenInclude(u => u.PersonalDetails) // Include PersonalDetails for the User
                .AsNoTracking()
                .ToListAsync();

            // Debugging: Check if PersonalDetails are loaded
            foreach (var record in records)
            {
                var user = record.User;
                var personalDetails = user?.PersonalDetails;
                Console.WriteLine($"User: {user?.Email}, StudentId: {personalDetails?.StudentId}");
            }

            return View("Index", records);
        }

        public IActionResult Create()
        {
            var model = new SubmittedHealthDetailsViewModel
            {
                BloodType = "",
                Allergies = "",
                EmergencyContactName = "",
                EmergencyContactRelationship = "",
                EmergencyContactPhone = ""
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SubmittedHealthDetailsViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Message"] = "Please correct the errors and try again.";
                TempData["MessageType"] = "warning";
                return RedirectToAction("Index", "Dashboard", new { activeTab = "healthrecords" });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["Message"] = "User not found.";
                TempData["MessageType"] = "error";
                return RedirectToAction("Index", "Dashboard", new { activeTab = "healthrecords" });
            }

            try
            {
                var submittedHealthDetails = new SubmittedHealthDetails
                {
                    UserId = user.Id,
                    BloodType = model.BloodType,
                    Allergies = model.Allergies,
                    EmergencyContactName = model.EmergencyContactName,
                    EmergencyContactRelationship = model.EmergencyContactRelationship,
                    EmergencyContactPhone = model.EmergencyContactPhone,
                    XRayFileUrl = await SaveFile(model.XRayFile),
                    MedicalCertificateUrl = await SaveFile(model.MedicalCertificate),
                    VaccinationRecordUrl = await SaveFile(model.VaccinationRecord),
                    OtherDocumentsUrl = await SaveFile(model.OtherDocuments),
                    SubmissionDate = DateTime.UtcNow
                };

                _context.SubmittedHealthDetails.Add(submittedHealthDetails);
                await _context.SaveChangesAsync();

                // Send notification to the user
                await SendSystemNotification(user.Id, "Your health details have been submitted successfully.");

                TempData["Message"] = "Health records submitted successfully!";
                TempData["MessageType"] = "success";
            }
            catch (Exception ex)
            {
                TempData["Message"] = "An error occurred while submitting the health records.";
                TempData["MessageType"] = "error";
            }

            return RedirectToAction("Index", "Dashboard", new { activeTab = "healthrecords" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var record = await _context.SubmittedHealthDetails.FindAsync(id);
            if (record == null)
            {
                TempData["Message"] = "Record not found.";
                TempData["MessageType"] = "error";
                return RedirectToAction("Index");
            }

            try
            {
                _context.SubmittedHealthDetails.Remove(record);
                await _context.SaveChangesAsync();

                // Send notification to the user
                await SendSystemNotification(record.UserId, "Your health record has been deleted.");

                TempData["Message"] = "Record deleted successfully!";
                TempData["MessageType"] = "success";
            }
            catch (Exception ex)
            {
                TempData["Message"] = "An error occurred while deleting the record.";
                TempData["MessageType"] = "error";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveField(int id, string fieldName)
        {
            var record = await _context.SubmittedHealthDetails.FindAsync(id);
            if (record == null)
            {
                return Json(new { success = false, message = "Record not found." });
            }

            switch (fieldName)
            {
                case "BloodType":
                    record.BloodTypeStatus = "Approved";
                    await UpdateHealthDetails(record.UserId, h => h.BloodType = record.BloodType);
                    break;
                case "Allergies":
                    record.AllergiesStatus = "Approved";
                    await UpdateHealthDetails(record.UserId, h => h.Allergies = record.Allergies);
                    break;
                case "EmergencyContactName":
                    record.EmergencyContactNameStatus = "Approved";
                    await UpdateHealthDetails(record.UserId, h => h.EmergencyContactName = record.EmergencyContactName);
                    break;
                case "EmergencyContactRelationship":
                    record.EmergencyContactRelationshipStatus = "Approved";
                    await UpdateHealthDetails(record.UserId, h => h.EmergencyContactRelationship = record.EmergencyContactRelationship);
                    break;
                case "EmergencyContactPhone":
                    record.EmergencyContactPhoneStatus = "Approved";
                    await UpdateHealthDetails(record.UserId, h => h.EmergencyContactPhone = record.EmergencyContactPhone);
                    break;
                case "XRayFile":
                    record.XRayFileStatus = "Approved";
                    await UpdateHealthDetails(record.UserId, h => h.XRayFileUrl = record.XRayFileUrl);
                    break;
                case "MedicalCertificate":
                    record.MedicalCertificateStatus = "Approved";
                    await UpdateHealthDetails(record.UserId, h => h.MedicalCertificateUrl = record.MedicalCertificateUrl);
                    break;
                case "VaccinationRecord":
                    record.VaccinationRecordStatus = "Approved";
                    await UpdateHealthDetails(record.UserId, h => h.VaccinationRecordUrl = record.VaccinationRecordUrl);
                    break;
                case "OtherDocuments":
                    record.OtherDocumentsStatus = "Approved";
                    await UpdateHealthDetails(record.UserId, h => h.OtherDocumentsUrl = record.OtherDocumentsUrl);
                    break;
                default:
                    return Json(new { success = false, message = "Invalid field name." });
            }

            _context.SubmittedHealthDetails.Update(record);
            await _context.SaveChangesAsync();

            // Send notification to the user
            await SendSystemNotification(record.UserId, $"{fieldName} in your health record has been approved.");

            TempData["RecordId"] = id; // Pass the record ID to TempData

            // Set TempData values
            TempData["Message"] = $"{fieldName} approved successfully!";
            TempData["MessageType"] = "success";

            // Include TempData values in the JSON response
            return Json(new
            {
                success = true,
                message = TempData["Message"],
                messageType = TempData["MessageType"],
                status = "Approved",
                fieldName
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectField(int id, string fieldName)
        {
            var record = await _context.SubmittedHealthDetails.FindAsync(id);
            if (record == null)
            {
                return Json(new { success = false, message = "Record not found." });
            }

            switch (fieldName)
            {
                case "BloodType":
                    record.BloodTypeStatus = "Rejected";
                    break;
                case "Allergies":
                    record.AllergiesStatus = "Rejected";
                    break;
                case "EmergencyContactName":
                    record.EmergencyContactNameStatus = "Rejected";
                    break;
                case "EmergencyContactRelationship":
                    record.EmergencyContactRelationshipStatus = "Rejected";
                    break;
                case "EmergencyContactPhone":
                    record.EmergencyContactPhoneStatus = "Rejected";
                    break;
                case "XRayFile":
                    record.XRayFileStatus = "Rejected";
                    break;
                case "MedicalCertificate":
                    record.MedicalCertificateStatus = "Rejected";
                    break;
                case "VaccinationRecord":
                    record.VaccinationRecordStatus = "Rejected";
                    break;
                case "OtherDocuments":
                    record.OtherDocumentsStatus = "Rejected";
                    break;
                default:
                    return Json(new { success = false, message = "Invalid field name." });
            }

            _context.SubmittedHealthDetails.Update(record);
            await _context.SaveChangesAsync();

            // Send notification to the user
            await SendSystemNotification(record.UserId, $"{fieldName} in your health record has been rejected.");

            TempData["Message"] = $"{fieldName} rejected successfully!";
            TempData["MessageType"] = "success";
            TempData["RecordId"] = id; // Pass the record ID to TempData

            return Json(new
            {
                success = true,
                message = TempData["Message"],
                messageType = TempData["MessageType"],
                status = "Rejected",
                fieldName
            });
        }

        private async Task UpdateHealthDetails(string userId, Action<HealthDetails> updateAction)
        {
            var healthDetails = await _context.HealthDetails.FirstOrDefaultAsync(h => h.UserId == userId);
            if (healthDetails == null)
            {
                healthDetails = new HealthDetails { UserId = userId };
                _context.HealthDetails.Add(healthDetails);
            }

            updateAction(healthDetails);
            await _context.SaveChangesAsync();
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

            // Send email notification
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                var subject = "System Notification";
                var emailMessage = $"You have received a new system notification:<br><br>{message}";
                await _emailService.SendEmailAsync(user.Email, subject, emailMessage);
            }
        }

        private async Task<string> SaveFile(IFormFile file)
        {
            if (file == null || file.Length == 0) return null;

            var uploadsFolder = Path.Combine("wwwroot", "uploads");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/uploads/{uniqueFileName}";
        }
    }
}