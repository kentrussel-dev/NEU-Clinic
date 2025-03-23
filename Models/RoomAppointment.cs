namespace WebApp.Models
{
    public class RoomAppointment
    {
        public int Id { get; set; }
        public string RoomName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Description { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public int UserLimit { get; set; } = 30; // Default limit

        // New property for QR code path
        public string QRCodePath { get; set; }

        // Many-to-many relationship with Users
        public ICollection<RoomAppointmentUser> RoomAppointmentUsers { get; set; } = new List<RoomAppointmentUser>();
    }

    // Join table for many-to-many relationship
    public class RoomAppointmentUser
    {
        public int RoomAppointmentId { get; set; }
        public RoomAppointment RoomAppointment { get; set; }

        public string UserId { get; set; }
        public Users User { get; set; }
    }
}