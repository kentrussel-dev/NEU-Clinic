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
        private readonly RoleManager<IdentityRole> _roleManager;

        public SubmittedHealthDetailsController(AppDbContext context, UserManager<Users> userManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<IActionResult> Index()
        {
            var records = await _context.SubmittedHealthDetails
               .AsNoTracking()
               .ToListAsync();

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
            if (user == null) return NotFound("User not found.");

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

        public async Task<IActionResult> ViewRecords()
        {
            var records = await _context.SubmittedHealthDetails
                .AsNoTracking()
                .Include(s => s.User)
                .ToListAsync();

            return View(records);
        }
    }
}
