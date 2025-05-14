namespace WebApp.Models.ViewModels
{
    public class AppointmentViewModel
    {
        public RoomAppointment Appointment { get; set; }
        public bool IsEnrolled { get; set; }
        public string CurrentUserId { get; set; }
        public List<EnrolledUserViewModel> EnrolledUsers { get; set; }
    }

    public class EnrolledUserViewModel
    {
        public string UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string ProfilePictureUrl { get; set; }
        public string ProfileUrl { get; set; }
        public AttendanceStatus Status { get; set; }
        public DateTime? StatusChangedAt { get; set; }
        public string StatusChangedBy { get; set; }
    }
}
