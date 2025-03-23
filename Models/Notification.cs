using System;

namespace WebApp.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public string UserId { get; set; } // Foreign key to the recipient user
        public string SenderEmail { get; set; } // Email of the sender
        public string Message { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation property to the recipient user
        public Users User { get; set; }
    }
}