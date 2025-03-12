using Microsoft.AspNetCore.Http;
using WebApp.Models; // Ensure this namespace includes SubmittedHealthDetails, Users, and PersonalDetails
using System.Collections.Generic;

namespace WebApp.ViewModels
{
    public class SubmittedHealthDetailsViewModel
    {
        // Fields for a single submission (Used when creating new records)
        public string? UserId { get; set; }
        public string? BloodType { get; set; }
        public string? Allergies { get; set; }
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactRelationship { get; set; }
        public string? EmergencyContactPhone { get; set; }
        public IFormFile? XRayFile { get; set; }
        public IFormFile? MedicalCertificate { get; set; }
        public IFormFile? VaccinationRecord { get; set; }
        public IFormFile? OtherDocuments { get; set; }
    }
}
