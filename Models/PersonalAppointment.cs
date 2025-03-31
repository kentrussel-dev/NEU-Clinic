using System;
using System.ComponentModel.DataAnnotations;

namespace WebApp.Models
{
    public class PersonalAppointment
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }
        public Users User { get; set; }

        [Required]
        public string Purpose { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime VisitationDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Add these new properties
        public string ApprovalStatus { get; set; } = "Pending"; // Default to Pending
        public string ProgressStatus { get; set; } = "Upcoming"; // Default to Upcoming
    }
}