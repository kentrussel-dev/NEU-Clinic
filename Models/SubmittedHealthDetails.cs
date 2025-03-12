using Microsoft.AspNetCore.Identity;

namespace WebApp.Models
{
    public class SubmittedHealthDetails
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public Users User { get; set; }
        public string? BloodType { get; set; }
        public string? Allergies { get; set; }
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactRelationship { get; set; }
        public string? EmergencyContactPhone { get; set; }
        public string? XRayFileUrl { get; set; }
        public string? MedicalCertificateUrl { get; set; }
        public string? VaccinationRecordUrl { get; set; }
        public string? OtherDocumentsUrl { get; set; }
        public DateTime SubmissionDate { get; set; } = DateTime.UtcNow;

    }
}