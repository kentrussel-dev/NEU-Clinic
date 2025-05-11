using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using System.Text.Json;

namespace WebApp.Models
{
    public class HealthDetails
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }  // Ensure this matches Users.Id type

        [ForeignKey("UserId")]
        public Users User { get; set; }

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

        // Store Health Alerts as a JSON string in the database
        public string HealthAlerts { get; set; } = "[]";  // Default value to avoid null issues

        // Helper method to deserialize Health Alerts
        [NotMapped]
        public List<string> HealthAlertsList
        {
            get
            {
                try
                {
                    return string.IsNullOrEmpty(HealthAlerts)
                        ? new List<string>()
                        : JsonSerializer.Deserialize<List<string>>(HealthAlerts) ?? new List<string>();
                }
                catch
                {
                    return new List<string>();  // Fallback to prevent errors
                }
            }
            set => HealthAlerts = JsonSerializer.Serialize(value);
        }

        public string? XRayFileUrl { get; set; }
        public string? MedicalCertificateUrl { get; set; }
        public string? VaccinationRecordUrl { get; set; }
        public string? OtherDocumentsUrl { get; set; }

        public DateTime? MedicalCertificateExpiryDate { get; set; }
        public DateTime? VaccinationRecordExpiryDate { get; set; }
        public DateTime? XRayExpiryDate { get; set; }
        public DateTime? DocumentSubmissionDate { get; set; }
        public DateTime? LastReminderSent { get; set; }
        public DateTime? LastExpiryReminderSent { get; set; }
    }
}