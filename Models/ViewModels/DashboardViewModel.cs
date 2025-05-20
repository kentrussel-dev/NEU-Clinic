namespace WebApp.Models.ViewModels
{
    public class DashboardViewModel
    {
        public List<RoomAppointment> RoomAppointments { get; set; } = new List<RoomAppointment>();
        public List<Users> Users { get; set; } = new List<Users>();
    }
}
