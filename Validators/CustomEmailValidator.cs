using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace WebApp.Validators
{
    public class CustomEmailValidator<TUser> : IUserValidator<TUser> where TUser : IdentityUser
    {
        private readonly string superAdminEmail;

        public CustomEmailValidator(IConfiguration configuration)
        {
            superAdminEmail = configuration["SuperAdmin:Email"]; // Get SuperAdmin email from appsettings.json
        }

        public Task<IdentityResult> ValidateAsync(UserManager<TUser> manager, TUser user)
        {
            if (!user.Email.EndsWith("@neu.edu.ph", StringComparison.OrdinalIgnoreCase)
                && !user.Email.Equals(superAdminEmail, StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(IdentityResult.Failed(new IdentityError
                {
                    Description = "Only @neu.edu.ph email addresses are allowed."
                }));
            }
            return Task.FromResult(IdentityResult.Success);
        }
    }
}
