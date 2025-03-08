using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using WebApp.Models;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace WebApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<Users> signInManager;
        private readonly UserManager<Users> userManager;
        private readonly string superAdminEmail;
        private readonly AppDbContext dbContext;
        private readonly QRCodeService qrCodeService;

        public AccountController(SignInManager<Users> signInManager, UserManager<Users> userManager, IConfiguration configuration, AppDbContext dbContext, QRCodeService qrCodeService)
        {
            this.signInManager = signInManager;
            this.userManager = userManager;
            this.superAdminEmail = configuration["SuperAdmin:Email"];
            this.dbContext = dbContext;
            this.qrCodeService = qrCodeService;
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
            if (info == null) return RedirectToAction(nameof(Login));

            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            if (!IsValidEmail(email))
            {
                TempData["ErrorMessage"] = "Only @neu.edu.ph emails are allowed.";
                return RedirectToAction(nameof(Login));
            }

            var user = await userManager.FindByEmailAsync(email);
            bool isNewUser = user == null;

            if (isNewUser)
                user = await CreateUserAsync(info, email);

            await EnsureQRCodeExists(user);
            await UpdateUserInfoIfChanged(user, info);

            await SignInUser(user);

            return RedirectToAction("Index", "Dashboard");
        }

        private bool IsValidEmail(string email)
        {
            return email != null && (email.EndsWith("@neu.edu.ph", System.StringComparison.OrdinalIgnoreCase) || email == superAdminEmail);
        }

        private async Task<Users> CreateUserAsync(ExternalLoginInfo info, string email)
        {
            var user = new Users
            {
                UserName = email,
                Email = email,
                FullName = info.Principal.FindFirstValue("urn:google:fullname"),
                ProfilePictureUrl = info.Principal.FindFirstValue("urn:google:picture") ?? "/default-profile.png",
                PhoneNumber = string.Empty,
                ESignaturePath = string.Empty
            };

            var createResult = await userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                foreach (var error in createResult.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                return null;
            }

            await userManager.AddLoginAsync(user, info);
            await AssignUserRole(user, email);
            await EnsureUserDetails(user.Id);

            return user;
        }

        private async Task AssignUserRole(Users user, string email)
        {
            string role = (email == superAdminEmail) ? "SuperAdmin" : "Student";
            await userManager.AddToRoleAsync(user, role);
        }

        private async Task EnsureUserDetails(string userId)
        {
            dbContext.PersonalDetails.Add(new PersonalDetails { UserId = userId });
            dbContext.HealthDetails.Add(new HealthDetails { UserId = userId });
            await dbContext.SaveChangesAsync();
        }

        private async Task EnsureQRCodeExists(Users user)
        {
            if (string.IsNullOrEmpty(user.QRCodePath))
            {
                user.QRCodePath = qrCodeService.GenerateQRCode(user.Id, user.FullName, user.Email);
                await userManager.UpdateAsync(user);
            }
        }

        private async Task UpdateUserInfoIfChanged(Users user, ExternalLoginInfo info)
        {
            bool needsUpdate = false;

            var fullName = info.Principal.FindFirstValue("urn:google:fullname");
            var profilePictureUrl = info.Principal.FindFirstValue("urn:google:picture") ?? "/default-profile.png";

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
                await userManager.UpdateAsync(user);
        }

        private async Task SignInUser(Users user)
        {
            var claims = new List<Claim>
            {
                new Claim("ProfilePictureUrl", user.ProfilePictureUrl ?? "/default-profile.png"),
                new Claim("FullName", user.FullName ?? "User")
            };

            await signInManager.SignInWithClaimsAsync(user, isPersistent: false, claims);
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
