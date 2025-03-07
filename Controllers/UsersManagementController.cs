using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging; // Added for logging
using WebApp.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApp.Controllers
{
    [Authorize(Roles = "SuperAdmin")]
    public class UsersManagementController : Controller
    {
        private readonly UserManager<Users> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<UsersManagementController> _logger; // Added logger

        public UsersManagementController(UserManager<Users> userManager, RoleManager<IdentityRole> roleManager, ILogger<UsersManagementController> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger; // Inject logger
        }

        public async Task<IActionResult> Index()
        {
            var users = _userManager.Users.ToList();
            var userRoles = new Dictionary<string, List<string>>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userRoles[user.Id] = roles.ToList();
            }

            ViewBag.UserRoles = userRoles;
            ViewBag.Roles = _roleManager.Roles.ToList();

            return View(users);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateUserRole(string userId, string newRole)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                _logger.LogWarning($"User role update failed: User ID {userId} not found.");
                return NotFound();
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, newRole);

            TempData["SuccessMessage"] = "User role updated successfully.";
            _logger.LogInformation($"User {user.UserName} (ID: {user.Id}) role updated to {newRole}.");

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                _logger.LogWarning($"User deletion failed: User ID {userId} not found.");
                return NotFound();
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "User deleted successfully.";
                _logger.LogInformation($"User {user.UserName} (ID: {user.Id}) deleted.");
                return RedirectToAction("Index");
            }

            TempData["ErrorMessage"] = "An error occurred while deleting the user.";
            _logger.LogError($"Error deleting user {user.UserName} (ID: {user.Id}).");
            return View("Error");
        }
    }
}
