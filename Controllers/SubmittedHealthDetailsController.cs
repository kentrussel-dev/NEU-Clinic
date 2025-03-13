using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
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

        public SubmittedHealthDetailsController(AppDbContext context, UserManager<Users> userManager)
        {
            _context = context;
            _userManager = userManager;
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
    }
}