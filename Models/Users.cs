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

        // Many-to-many relationship with RoomAppointment
        public ICollection<RoomAppointmentUser> RoomAppointmentUsers { get; set; } = new List<RoomAppointmentUser>();

        // One-to-many relationship with Notification
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}