using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApp.Models
{
    public class HealthDetails
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public Users User { get; set; }  // Foreign Key Reference

        public string? BloodType { get; set; }
        public string? Allergies { get; set; }
        public string? MedicalNotes { get; set; }
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactRelationship { get; set; }
        public string? EmergencyContactPhone { get; set; }
        public string? ChronicConditions { get; set; }
        public string? Medications { get; set; }
        public string? ImmunizationHistory { get; set; }
        public string? RecentCheckups { get; set; }
        public string? ActivityRestrictions { get; set; }
        public string? DietaryRestrictions { get; set; }
        public string? MentalHealthNotes { get; set; }
        public string? HealthAlerts { get; set; }  // JSON format if multiple alerts

        // Health Document URLs
        public string? XRayFileUrl { get; set; }
        public string? MedicalCertificateUrl { get; set; }
        public string? VaccinationRecordUrl { get; set; }
        public string? OtherDocumentsUrl { get; set; }
    }
}
