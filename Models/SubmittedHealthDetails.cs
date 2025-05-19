using WebApp.Models;
using System;

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

        // Status for each field
        public string BloodTypeStatus { get; set; } = "Pending";
        public string AllergiesStatus { get; set; } = "Pending";
        public string EmergencyContactNameStatus { get; set; } = "Pending";
        public string EmergencyContactRelationshipStatus { get; set; } = "Pending";
        public string EmergencyContactPhoneStatus { get; set; } = "Pending";
        public string XRayFileStatus { get; set; } = "Pending";
        public string MedicalCertificateStatus { get; set; } = "Pending";
        public string VaccinationRecordStatus { get; set; } = "Pending";
        public string OtherDocumentsStatus { get; set; } = "Pending";
    }
}