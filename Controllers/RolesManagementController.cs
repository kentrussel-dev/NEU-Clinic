using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApp.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApp.Data;

namespace WebApp.Controllers
{
    [Authorize(Roles = "SuperAdmin")]
    public class RolesManagementController : Controller
    {
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly UserManager<Users> userManager;
        private readonly AppDbContext _context;

        // Temporary dictionary to store role permissions (simulating database behavior)
        private static Dictionary<string, List<string>> tempRolePermissions = new();

        public RolesManagementController(
            RoleManager<IdentityRole> roleManager,
            UserManager<Users> userManager,
            AppDbContext context)
        {
            this.roleManager = roleManager;
            this.userManager = userManager;
            _context = context;
        }

        public IActionResult Index()
        {
            var roles = roleManager.Roles.ToList(); // Get all roles
            var allPermissions = new List<string> { "ManageUsers", "ManageRoles", "ViewReports", "EditContent" }; // Example permissions

            // Ensure each role has a permissions list in temp storage
            foreach (var role in roles)
            {
                if (!tempRolePermissions.ContainsKey(role.Name))
                {
                    tempRolePermissions[role.Name] = new List<string>(); // Initialize empty permissions
                }
            }

            ViewBag.AllPermissions = allPermissions;
            ViewBag.RolePermissions = tempRolePermissions; // Pass temp permissions to view
            return View(roles);
        }

        [HttpPost]
        public IActionResult UpdateRolePermissions(string roleId, List<string> selectedPermissions)
        {
            var role = roleManager.Roles.FirstOrDefault(r => r.Id == roleId);
            if (role == null)
                return NotFound();

            // Update temporary permissions (not modifying database)
            tempRolePermissions[role.Name] = selectedPermissions ?? new List<string>();

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRole(string roleId)
        {
            var role = await roleManager.FindByIdAsync(roleId);
            if (role == null)
                return NotFound();

            // Prevent deletion of SuperAdmin role
            if (role.Name == "SuperAdmin")
                return BadRequest("SuperAdmin role cannot be deleted.");

            var result = await roleManager.DeleteAsync(role);
            if (result.Succeeded)
            {
                // Remove role from temporary permissions storage
                tempRolePermissions.Remove(role.Name);

                // Send notification to the SuperAdmin
                var superAdmin = await userManager.GetUsersInRoleAsync("SuperAdmin");
                if (superAdmin.Any())
                {
                    foreach (var admin in superAdmin)
                    {
                        await SendSystemNotification(admin.Id, $"Role '{role.Name}' has been deleted.");
                    }
                }

                return RedirectToAction("Index");
            }

            return BadRequest("Failed to delete role.");
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
        }
    }
}