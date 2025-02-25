using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebApp.Data;
using WebApp.Models;
using WebApp.Validators;

var builder = WebApplication.CreateBuilder(args);

// Add DB Context
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure Identity with Google Authentication
builder.Services.AddIdentity<Users, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;             
    options.Password.RequireLowercase = false;       
    options.Password.RequireUppercase = false;       
    options.Password.RequireNonAlphanumeric = false; 
    options.Password.RequiredUniqueChars = 0;        
})
.AddUserValidator<CustomEmailValidator<Users>>()
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
})
.AddGoogle(options =>
{
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
    options.SignInScheme = IdentityConstants.ExternalScheme;

    // Redirect to profile creation after Google login
    options.Events.OnCreatingTicket = async context =>
    {
        var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<Users>>();
        var signInManager = context.HttpContext.RequestServices.GetRequiredService<SignInManager<Users>>();
        var userEmail = context.Principal.FindFirstValue(System.Security.Claims.ClaimTypes.Email);

        var user = await userManager.FindByEmailAsync(userEmail);
        if (user == null)
        {
            context.Response.Redirect("/Account/CreateProfile");
        }
    };
});

// Configure application cookie
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Cookie.Name = "WebAppAuthCookie";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    options.SlidingExpiration = true;
});

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Middleware configuration
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultControllerRoute();

app.Run();
