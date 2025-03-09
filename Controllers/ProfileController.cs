using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using WebApp.Models;
using WebApp.Data;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WebApp.ViewModels;

public class ProfileController : Controller
{
    private readonly UserManager<Users> _userManager;
    private readonly AppDbContext _context;
    private readonly ILogger<ProfileController> _logger;

    public ProfileController(UserManager<Users> userManager, AppDbContext context, ILogger<ProfileController> logger)
    {
        _userManager = userManager;
        _context = context;
        _logger = logger;
    }

    // New action to display the profile view for a specific user
    [HttpGet("profile/{userId}")]
    public async Task<IActionResult> Index(string userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            TempData["ErrorMessage"] = "User ID is required.";
            _logger.LogWarning("Profile view failed: User ID is missing.");
            return NotFound("User ID is required.");
        }

        // Fetch the user details
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            TempData["ErrorMessage"] = "User not found.";
            _logger.LogWarning($"Profile view failed: User with ID {userId} not found.");
            return NotFound("User not found.");
        }

        // Fetch the personal details
        var personalDetails = await _context.PersonalDetails.FirstOrDefaultAsync(p => p.UserId == userId);
        if (personalDetails == null)
        {
            TempData["ErrorMessage"] = "Personal details not found.";
            _logger.LogWarning($"Profile view failed: Personal details for user with ID {userId} not found.");
            return NotFound("Personal details not found.");
        }

        // Fetch the Health Details
        var healthDetails = await _context.HealthDetails.FirstOrDefaultAsync(p => p.UserId == userId);
        if (healthDetails == null)
        {
            TempData["ErrorMessage"] = "Health Details not found.";
            _logger.LogWarning($"Profile view failed: Health Details for user with ID {userId} not found.");
            return NotFound("Health Details not found.");
        }

        var profileViewModel = new ProfileViewModel
        {
            User = user,
            PersonalDetails = personalDetails,
            HealthDetails = healthDetails
        };

        return View(profileViewModel);
    }

    [HttpPost]
    public async Task<IActionResult> UpdatePersonalDetails(PersonalDetails model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            TempData["ErrorMessage"] = "User not found.";
            _logger.LogWarning("User update failed: User not found.");
            return NotFound("User not found.");
        }

        var personalDetails = await _context.PersonalDetails.FirstOrDefaultAsync(p => p.UserId == user.Id);
        if (personalDetails == null)
        {
            personalDetails = new PersonalDetails { UserId = user.Id };
            _context.PersonalDetails.Add(personalDetails);
        }

        personalDetails.DateOfBirth = model.DateOfBirth;
        personalDetails.Department = model.Department;
        personalDetails.StudentId = model.StudentId;
        personalDetails.Address = model.Address;
        personalDetails.YearLevel = model.YearLevel;
        personalDetails.Course = model.Course;

        try
        {
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Profile updated successfully.";
            _logger.LogInformation($"User {user.UserName} (ID: {user.Id}) updated their personal details.");
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "An error occurred while updating profile.";
            _logger.LogError($"Error updating profile for user {user.UserName} (ID: {user.Id}): {ex.Message}");
        }

        return RedirectToAction("Index", "Dashboard", new { activeTab = "profile" });
    }
}