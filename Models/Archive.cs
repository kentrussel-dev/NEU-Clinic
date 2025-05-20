// Models/Archive.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApp.Models
{
    public class Archive
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public Users User { get; set; }

        [Required]
        public string DocumentType { get; set; } // "XRay", "MedicalCertificate", "VaccinationRecord", etc.

        public string FileUrl { get; set; }

        public DateTime OriginalExpiryDate { get; set; }

        public DateTime ArchivedDate { get; set; } = DateTime.UtcNow;

        public string ArchivedBy { get; set; } // UserId of who archived it (or "System" for auto-archive)
    }
}