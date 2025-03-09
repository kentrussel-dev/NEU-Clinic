using System.ComponentModel.DataAnnotations;
using WebApp.Models;

namespace WebApp.ViewModels
{
    public class ProfileViewModel
    {
        public Users User { get; set; } // Represents the user's basic information
        public PersonalDetails PersonalDetails { get; set; } // Represents the user's personal details
        public HealthDetails HealthDetails { get; set; } // Represents the user's health details
    }
}

