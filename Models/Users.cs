﻿using Microsoft.AspNetCore.Identity;

namespace WebApp.Models
{
    public class Users : IdentityUser
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? StudentId { get; set; }
        public string? Address { get; set; }
        public string? ESignaturePath { get; set; }
        public string? ProfilePictureUrl { get; set; }
    }

}
