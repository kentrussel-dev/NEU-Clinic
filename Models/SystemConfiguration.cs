using System.ComponentModel.DataAnnotations;

namespace WebApp.Models
{
    public class SystemConfiguration
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public DateTime HealthDocumentsExpiryDate { get; set; }

        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}
