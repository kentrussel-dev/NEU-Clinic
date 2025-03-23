using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using WebApp.Data;
using WebApp.Models;
using WebApp.Validators;

var builder = WebApplication.CreateBuilder(args);

// ✅ Add Database Context
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ✅ Register QRCodeService for Dependency Injection
builder.Services.AddScoped<QRCodeService>();

// ✅ Register EmailService for Dependency Injection
builder.Services.AddScoped<EmailService>();

// ✅ Register NotificationService for Dependency Injection
builder.Services.AddScoped<NotificationService>();

// ✅ Configure Identity with Google Authentication
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

// ✅ Register UserManager<Users> for Dependency Injection
builder.Services.AddScoped<UserManager<Users>>();

// ✅ Register ILogger for Dependency Injection
builder.Services.AddLogging();

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
})
.AddGoogle(options =>
{
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
    options.CallbackPath = new PathString("/signin-google");
    options.Scope.Add("profile");
    options.ClaimActions.MapJsonKey("urn:google:picture", "picture", "url");
    options.ClaimActions.MapJsonKey("urn:google:fullname", "name");

    options.SignInScheme = IdentityConstants.ExternalScheme;

    // ✅ Fix redirect issue (force HTTPS)
    options.Events.OnRedirectToAuthorizationEndpoint = context =>
    {
        string httpsRedirectUri = context.RedirectUri.Replace("http://", "https://");
        context.Response.Redirect(httpsRedirectUri + "&prompt=select_account");
        return Task.CompletedTask;
    };
});

// ✅ Configure Application Cookie Settings
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

// ✅ Force HTTPS handling when behind a reverse proxy (like Ngrok)
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedProto
});

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection(); // Ensure HTTPS is enforced
}

// ✅ Ensure role seeding is called inside an async method
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await SeedRolesAndSuperAdminAsync(services);
}

// ✅ Middleware Configuration
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultControllerRoute();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{activeTab?}");

app.Run();

// =============================
// ✅ ASYNC ROLE SEEDING LOGIC
// =============================
async Task SeedRolesAndSuperAdminAsync(IServiceProvider serviceProvider)
{
    var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = serviceProvider.GetRequiredService<UserManager<Users>>();
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();

    string[] roleNames = { "SuperAdmin", "Admin", "Student", "MedicalStaff" };

    foreach (var roleName in roleNames)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }

    string superAdminEmail = configuration["SuperAdmin:Email"];
    if (!string.IsNullOrEmpty(superAdminEmail))
    {
        var superAdmin = await userManager.FindByEmailAsync(superAdminEmail);
        if (superAdmin != null && !await userManager.IsInRoleAsync(superAdmin, "SuperAdmin"))
        {
            await userManager.AddToRoleAsync(superAdmin, "SuperAdmin");
        }
    }
}