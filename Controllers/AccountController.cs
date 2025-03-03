using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using WebApp.Models;
using WebApp.ViewModels;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace WebApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<Users> signInManager;
        private readonly UserManager<Users> userManager;

        public AccountController(SignInManager<Users> signInManager, UserManager<Users> userManager)
        {
            this.signInManager = signInManager;
            this.userManager = userManager;
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
            if (email == null || !email.EndsWith("@neu.edu.ph", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Only @neu.edu.ph email addresses are allowed for Google login.";
                return RedirectToAction(nameof(Login));
            }

            var profilePictureUrl = info.Principal.FindFirstValue("urn:google:picture") ?? "/default-profile.png";
            var user = await userManager.FindByEmailAsync(email);

            if (user == null)
            {
                user = new Users
                {
                    UserName = email,
                    Email = email,
                    PhoneNumber = string.Empty,
                    ESignaturePath = string.Empty,
                    ProfilePictureUrl = profilePictureUrl
                };

                var createResult = await userManager.CreateAsync(user);
                if (createResult.Succeeded)
                {
                    await userManager.AddLoginAsync(user, info);
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
                if (!string.IsNullOrEmpty(profilePictureUrl) && user.ProfilePictureUrl != profilePictureUrl)
                {
                    user.ProfilePictureUrl = profilePictureUrl;
                    await userManager.UpdateAsync(user);
                }
            }

            var claims = new List<Claim>
            {
                new Claim("ProfilePictureUrl", user.ProfilePictureUrl ?? "/default-profile.png")
            };
            await signInManager.SignInWithClaimsAsync(user, isPersistent: false, claims);

            // Always redirect to the dashboard
            return RedirectToAction("Index", "Dashboard");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            // Sign out from ASP.NET Identity
            await signInManager.SignOutAsync();

            // Clear external authentication cookies
            await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            // Invalidate session cookie
            Response.Cookies.Delete(".AspNetCore.Identity.Application");

            // Redirect to login page (forces Google to ask for account selection next time)
            return RedirectToAction("Login", "Account");
        }

    }
}