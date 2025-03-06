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

namespace WebApp.Controllers
{
    [Authorize(Roles = "SuperAdmin")]
    public class UserHealthManagementController : Controller
    {
        private readonly UserManager<Users> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly AppDbContext _context;
        private readonly ILogger<UserHealthManagementController> _logger;

        public UserHealthManagementController(
            UserManager<Users> userManager,
            RoleManager<IdentityRole> roleManager,
            AppDbContext context,
            ILogger<UserHealthManagementController> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            var userRoles = new Dictionary<string, List<string>>();
            var userHealthDetails = new Dictionary<string, HealthDetails>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userRoles[user.Id] = roles.ToList();

                var healthDetails = await _context.HealthDetails
                    .FirstOrDefaultAsync(h => h.UserId == user.Id);
                userHealthDetails[user.Id] = healthDetails ?? new HealthDetails { UserId = user.Id };
            }

            ViewBag.UserRoles = userRoles;
            ViewBag.Roles = _roleManager.Roles.ToList();
            ViewBag.UserHealthDetails = userHealthDetails;

            return View(users);
        }

        public async Task<IActionResult> UpdateHealthDetails(HealthDetails model)
        {
            if (model == null)
            {
                TempData["ErrorMessage"] = "Invalid data.";
                return RedirectToAction("Index");
            }

            // Ensure the UserId is present
            if (string.IsNullOrEmpty(model.UserId))
            {
                TempData["ErrorMessage"] = "User selection is required.";
                return RedirectToAction("Index");
            }

            var existingHealthDetails = await _context.HealthDetails
                .FirstOrDefaultAsync(h => h.UserId == model.UserId);

            if (existingHealthDetails == null)
            {
                // If no existing record, create a new entry
                _context.HealthDetails.Add(model);
            }
            else
            {
                // Update fields while keeping the same UserId
                existingHealthDetails.BloodType = model.BloodType;
                existingHealthDetails.Allergies = model.Allergies;
                existingHealthDetails.MedicalNotes = model.MedicalNotes;
                existingHealthDetails.EmergencyContactName = model.EmergencyContactName;
                existingHealthDetails.EmergencyContactRelationship = model.EmergencyContactRelationship;
                existingHealthDetails.EmergencyContactPhone = model.EmergencyContactPhone;
                existingHealthDetails.ChronicConditions = model.ChronicConditions;
                existingHealthDetails.Medications = model.Medications;
                existingHealthDetails.ImmunizationHistory = model.ImmunizationHistory;
                existingHealthDetails.RecentCheckups = model.RecentCheckups;
                existingHealthDetails.ActivityRestrictions = model.ActivityRestrictions;
                existingHealthDetails.DietaryRestrictions = model.DietaryRestrictions;
                existingHealthDetails.MentalHealthNotes = model.MentalHealthNotes;
                existingHealthDetails.HealthAlerts = model.HealthAlerts;
                existingHealthDetails.XRayFileUrl = model.XRayFileUrl;
                existingHealthDetails.MedicalCertificateUrl = model.MedicalCertificateUrl;
                existingHealthDetails.VaccinationRecordUrl = model.VaccinationRecordUrl;
                existingHealthDetails.OtherDocumentsUrl = model.OtherDocumentsUrl;

                _context.HealthDetails.Update(existingHealthDetails);
            }

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Health details updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error saving health details: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while updating health details.";
            }

            return RedirectToAction("Index");
        }

    }
}