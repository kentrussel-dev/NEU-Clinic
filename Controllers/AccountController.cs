﻿using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using WebApp.Models;
using WebApp.ViewModels;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

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

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await userManager.Users
                    .FirstOrDefaultAsync(u => u.Email == model.Email);

                if (user != null)
                {
                    var result = await signInManager.PasswordSignInAsync(user.UserName, model.Password, model.RememberMe, false);
                    if (result.Succeeded)
                    {
                        return RedirectToAction("Index", "Home");
                    }
                }
                ModelState.AddModelError("", "Email or password is incorrect.");
            }
            return View(model);
        }

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

            // 🎯 Extracting profile picture URL from Google claims
            var profilePictureUrl = info.Principal.FindFirstValue("urn:google:picture") ?? "/default-profile.png";

            var user = await userManager.FindByEmailAsync(email);

            if (user == null)
            {
                // 🧱 Create new user with basic details
                user = new Users
                {
                    UserName = email,
                    Email = email,
                    StudentId = string.Empty,
                    Address = string.Empty,
                    PhoneNumber = string.Empty,
                    FirstName = string.Empty,
                    LastName = string.Empty,
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
                // 🖼️ Update profile picture if changed
                if (!string.IsNullOrEmpty(profilePictureUrl) && user.ProfilePictureUrl != profilePictureUrl)
                {
                    user.ProfilePictureUrl = profilePictureUrl;
                    await userManager.UpdateAsync(user);
                }
            }

            // ✨ Add ProfilePictureUrl claim during sign-in
            var claims = new List<Claim>
            {
                new Claim("ProfilePictureUrl", user.ProfilePictureUrl ?? "/default-profile.png")
            };
            await signInManager.SignInWithClaimsAsync(user, isPersistent: false, claims);

            if (string.IsNullOrEmpty(user.StudentId) ||
                string.IsNullOrEmpty(user.Address) ||
                string.IsNullOrEmpty(user.PhoneNumber) ||
                string.IsNullOrEmpty(user.FirstName) ||
                string.IsNullOrEmpty(user.LastName) ||
                string.IsNullOrEmpty(user.PasswordHash) ||
                string.IsNullOrEmpty(user.UserName)||
                string.IsNullOrEmpty(user.ESignaturePath))
            {
                return RedirectToAction("CompleteProfile", "Account", new { returnUrl });
            }

            return RedirectToAction("Index", "Dashboard");
        }


        [HttpGet]
        [HttpGet]
        public async Task<IActionResult> CompleteProfile(string returnUrl = null)
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null ||
               (!string.IsNullOrEmpty(user.StudentId) &&
                !string.IsNullOrEmpty(user.Address) &&
                !string.IsNullOrEmpty(user.PhoneNumber) &&
                !string.IsNullOrEmpty(user.FirstName) &&
                !string.IsNullOrEmpty(user.LastName) &&
                !string.IsNullOrEmpty(user.PasswordHash) &&
                !string.IsNullOrEmpty(user.UserName) &&
                !string.IsNullOrEmpty(user.ESignaturePath)))
            {
                return RedirectToAction("Index", "Dashboard"); // ✅ Redirect to Dashboard
            }

            ViewBag.ReturnUrl = returnUrl;
            return View();
        }


        public async Task<IActionResult> CompleteProfile(ProfileViewModel model, string returnUrl = null)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction(nameof(Login));

            var result = await userManager.RemovePasswordAsync(user);
            if (result.Succeeded)
            {
                result = await userManager.AddPasswordAsync(user, model.Password);
            }

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.StudentId = model.StudentId;
            user.UserName = model.Username;
            user.Address = model.Address;
            user.PhoneNumber = model.PhoneNumber;

            if (model.ESignature != null && model.ESignature.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                var filePath = Path.Combine(uploadsFolder, $"{user.Id}_signature.png");
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ESignature.CopyToAsync(stream);
                }
                user.ESignaturePath = $"/uploads/{user.Id}_signature.png";
            }

            await userManager.UpdateAsync(user);
            await signInManager.SignOutAsync();
            await signInManager.PasswordSignInAsync(user.UserName, model.Password, false, false);

            // 🎯 Redirecting to Dashboard after successful profile completion
            return RedirectToAction("Index", "Dashboard");
        }


        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                Users users = new Users()
                {
                    FirstName = model.Name,
                    Email = model.Email,
                    UserName = model.Email,
                    StudentId = string.Empty,
                    Address = string.Empty,
                    PhoneNumber = string.Empty,
                    ESignaturePath = string.Empty
                };

                var result = await userManager.CreateAsync(users, model.Password);
                if (result.Succeeded)
                {
                    var passwordResult = await userManager.AddPasswordAsync(users, model.Password);
                    if (!passwordResult.Succeeded)
                    {
                        foreach (var error in passwordResult.Errors)
                            ModelState.AddModelError(string.Empty, error.Description);
                        return View(model);
                    }
                    return RedirectToAction("Login", "Account");
                }

                foreach (var err in result.Errors)
                    ModelState.AddModelError("", err.Description);
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }
    }
}
