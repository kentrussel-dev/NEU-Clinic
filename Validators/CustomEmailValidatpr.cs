using Microsoft.AspNetCore.Identity;

namespace WebApp.Validators
{
    public class CustomEmailValidator<TUser> : IUserValidator<TUser> where TUser : IdentityUser
    {
        public Task<IdentityResult> ValidateAsync(UserManager<TUser> manager, TUser user)
        {
            if (!user.Email.EndsWith("@neu.edu.ph", StringComparison.OrdinalIgnoreCase))
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
