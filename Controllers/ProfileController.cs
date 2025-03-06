using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using WebApp.Models;
using WebApp.Data;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public class ProfileController : Controller
{
    private readonly UserManager<Users> _userManager;
    private readonly AppDbContext _context;

    public ProfileController(UserManager<Users> userManager, AppDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> UpdatePersonalDetails(PersonalDetails model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
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

        await _context.SaveChangesAsync();
        return RedirectToAction("Index", "Dashboard", new { activeTab = "profile" });
    }
}