using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging; // Added for logging
using WebApp.Models;
using WebApp.Data;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

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
