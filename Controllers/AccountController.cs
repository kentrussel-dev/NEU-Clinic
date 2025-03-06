using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using WebApp.Models;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;

namespace WebApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<Users> signInManager;
        private readonly UserManager<Users> userManager;
        private readonly string superAdminEmail;

        public AccountController(SignInManager<Users> signInManager, UserManager<Users> userManager, IConfiguration configuration)
        {
            this.signInManager = signInManager;
            this.userManager = userManager;
            this.superAdminEmail = configuration["SuperAdmin:Email"];
        }

        public IActionResult Login() => View();

        [HttpGet]
        public IActionResult ExternalLogin(string provider, string returnUrl = null)
        {
            var redirectUrl = Url.Action("ExternalLoginCallback", "Account", new { ReturnUrl = returnUrl });
            var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider);
        }

        [HttpGet]
        public async Task<IActionResult> ExternalLoginCallback(string returnUrl = null, string remoteError = null)
        {
            returnUrl ??= Url.Content("~/");

            if (remoteError != null)
            {
                ModelState.AddModelError(string.Empty, $"Error from external provider: {remoteError}");
                return View(nameof(Login));
            }

            var info = await signInManager.GetExternalLoginInfoAsync();
            if (info == null)
                return RedirectToAction(nameof(Login));

            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            var fullName = info.Principal.FindFirstValue("urn:google:fullname");
            var profilePictureUrl = info.Principal.FindFirstValue("urn:google:picture") ?? "/default-profile.png";

            if (email == null || (!email.EndsWith("@neu.edu.ph", StringComparison.OrdinalIgnoreCase) && email != superAdminEmail))
            {
                TempData["ErrorMessage"] = "Only @neu.edu.ph email addresses are allowed for Google login.";
                return RedirectToAction(nameof(Login));
            }

            var user = await userManager.FindByEmailAsync(email);

            if (user == null)
            {
                user = new Users
                {
                    UserName = email,
                    Email = email,
                    FullName = fullName,
                    ProfilePictureUrl = profilePictureUrl,
                    PhoneNumber = string.Empty,
                    ESignaturePath = string.Empty
                };

                var createResult = await userManager.CreateAsync(user);
                if (createResult.Succeeded)
                {
                    await userManager.AddLoginAsync(user, info);

                    // ✅ Assign role based on user type
                    if (email == superAdminEmail)
                        await userManager.AddToRoleAsync(user, "SuperAdmin");
                    else
                        await userManager.AddToRoleAsync(user, "Student");
                }
                else
                {
                    foreach (var error in createResult.Errors)
                        ModelState.AddModelError(string.Empty, error.Description);
                    return View(nameof(Login));
                }
            }
            else
            {
                // ✅ Ensure profile picture & full name are updated
                bool needsUpdate = false;

                if (!string.IsNullOrEmpty(fullName) && user.FullName != fullName)
                {
                    user.FullName = fullName;
                    needsUpdate = true;
                }

                if (!string.IsNullOrEmpty(profilePictureUrl) && user.ProfilePictureUrl != profilePictureUrl)
                {
                    user.ProfilePictureUrl = profilePictureUrl;
                    needsUpdate = true;
                }

                if (needsUpdate)
                {
                    await userManager.UpdateAsync(user);
                }

                // ✅ Prevent duplicate role assignments
                var userRoles = await userManager.GetRolesAsync(user);
                if (email == superAdminEmail && !userRoles.Contains("SuperAdmin"))
                {
                    await userManager.AddToRoleAsync(user, "SuperAdmin");
                }
            }

            var claims = new List<Claim>
            {
                new Claim("ProfilePictureUrl", user.ProfilePictureUrl ?? "/default-profile.png"),
                new Claim("FullName", user.FullName ?? "User") // ✅ Include Full Name in Claims
            };

            await signInManager.SignInWithClaimsAsync(user, isPersistent: false, claims);

            return RedirectToAction("Index", "Dashboard");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await signInManager.SignOutAsync();
            await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            Response.Cookies.Delete(".AspNetCore.Identity.Application");
            return RedirectToAction("Login", "Account");
        }
    }
}
