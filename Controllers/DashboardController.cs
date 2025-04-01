using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Users> _userManager;

        public DashboardController(AppDbContext context, UserManager<Users> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string activeTab = "home")
        {
            // Get the current user
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account"); // Redirect to login if user is not found
            }

            // Get the roles of the current user
            var roles = await _userManager.GetRolesAsync(user);
            var isStudent = roles.Contains("Student");

            // Set the active tab in ViewBag
            ViewBag.ActiveTab = activeTab;

            // Fetch data based on the active tab
            if (isStudent && activeTab == "appointments")
            {
                // Fetch available appointments (where the user limit has not been reached)
                var availableAppointments = await _context.RoomAppointments
                    .Include(ra => ra.RoomAppointmentUsers)
                    .Where(ra => ra.RoomAppointmentUsers.Count < ra.UserLimit)
                    .ToListAsync();

                // Pass the list of available appointments to the view
                return View(availableAppointments);
            }

            // Default view for other tabs
            return View();
        }
    }
}