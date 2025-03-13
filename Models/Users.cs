using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace WebApp.Models
{
    public class Users : IdentityUser
    {
        public string? FullName { get; set; }
        public string? ESignaturePath { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public string? QRCodePath { get; set; }

        public PersonalDetails PersonalDetails { get; set; } 
        public HealthDetails HealthDetails { get; set; }
        public ICollection<SubmittedHealthDetails> SubmittedHealthDetails { get; set; }
    }
}
