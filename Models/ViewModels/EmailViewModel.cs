using System.ComponentModel.DataAnnotations;


public class EmailViewModel
{
    [Required]
    public List<string> UserIds { get; set; } = new List<string>();

    [Required]
    [Display(Name = "Subject")]
    public string Subject { get; set; }

    [Required]
    [Display(Name = "Message")]
    public string Message { get; set; }

    [Display(Name = "Attachments")]
    public List<IFormFile> Attachments { get; set; }
}
