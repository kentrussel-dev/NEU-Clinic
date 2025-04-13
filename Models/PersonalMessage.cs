using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApp.Models
{
    public class PersonalMessage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string SenderId { get; set; }

        [Required]
        public string ReceiverId { get; set; }

        [Required]
        public string Content { get; set; }

        public DateTime SentAt { get; set; } = DateTime.Now;

        public DateTime? ReadAt { get; set; }

        [ForeignKey("SenderId")]
        public virtual Users Sender { get; set; }

        [ForeignKey("ReceiverId")]
        public virtual Users Receiver { get; set; }
    }
}