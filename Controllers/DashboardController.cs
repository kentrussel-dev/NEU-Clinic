using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;
using WebApp.Models.ViewModels;
namespace WebApp.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Users> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public DashboardController(
            AppDbContext context,
            UserManager<Users> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<IActionResult> Index(string activeTab = "home")
        {
            // Get the current user
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Get the roles of the current user
            var roles = await _userManager.GetRolesAsync(user);
            var isSuperAdmin = roles.Contains("SuperAdmin");
            var isAdmin = roles.Contains("Admin");
            var isStudent = roles.Contains("Student");

            // Set the active tab in ViewBag
            ViewBag.ActiveTab = activeTab;

            // Create a ViewModel to hold both types of data
            var viewModel = new DashboardViewModel
            {
                RoomAppointments = await _context.RoomAppointments
                    .Include(ra => ra.RoomAppointmentUsers)
                    .Where(ra => ra.RoomAppointmentUsers.Count < ra.UserLimit)
                    .ToListAsync()
            };

            // If the active tab is usermanagement and user is SuperAdmin, load user data
            if (activeTab == "usermanagement" && isSuperAdmin)
            {
                var users = await _userManager.Users.ToListAsync();
                var userRoles = new Dictionary<string, List<string>>();

                foreach (var u in users)
                {
                    var userRolesList = await _userManager.GetRolesAsync(u);
                    userRoles[u.Id] = userRolesList.ToList();
                }

                viewModel.Users = users; // Update with actual users
                ViewBag.UserRoles = userRoles;
                ViewBag.Roles = await _roleManager.Roles.ToListAsync();
            }

            return View(viewModel);
        }

        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> LoadUserManagementData()
        {
            var users = await _userManager.Users
                .Include(u => u.PersonalDetails)
                .ToListAsync();

            var userRoles = new Dictionary<string, List<string>>();
            foreach (var user in users)
            {
                userRoles[user.Id] = (await _userManager.GetRolesAsync(user)).ToList();
            }

            ViewBag.UserRoles = userRoles;
            ViewBag.Roles = await _roleManager.Roles.ToListAsync();

            return PartialView("Admin/_UsersManagementPartial", users);
        }
    }
}