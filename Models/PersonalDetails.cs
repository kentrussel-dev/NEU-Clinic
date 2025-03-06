using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApp.Models
{
    public class PersonalDetails
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public Users User { get; set; }  // Foreign Key Reference

        public DateTime? DateOfBirth { get; set; }
        public string? Department { get; set; }
        public string? StudentId { get; set; }
        public string? Address { get; set; }
        public string? YearLevel { get; set; }
        public string? Course { get; set; }
    }
}
