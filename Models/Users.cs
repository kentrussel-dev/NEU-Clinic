using Microsoft.AspNetCore.Identity;

namespace WebApp.Models
{
    public class Users : IdentityUser
    {
        public string? FullName { get; set; }
        public string? ESignaturePath { get; set; }
        public string? ProfilePictureUrl { get; set; }
    }
}
