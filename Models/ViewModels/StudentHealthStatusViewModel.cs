namespace WebApp.Models.ViewModels
{
    public class StudentHealthStatusViewModel
    {
        public List<StudentHealthStatusModel> Students { get; set; } = new List<StudentHealthStatusModel>();
        public string FilterRequirement { get; set; }
    }
    public class StudentHealthStatusModel
    {
        public string Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Department { get; set; }
        public string ProfilePictureUrl { get; set; }
        public string BloodType { get; set; }
        public bool HasEmergencyContact { get; set; }
        public bool HasXRay { get; set; }
        public bool HasMedicalCertificate { get; set; }
        public bool HasVaccinationRecord { get; set; }
        public List<string> HealthAlerts { get; set; } = new List<string>();
        public int CompletionPercentage { get; set; }
    }
}