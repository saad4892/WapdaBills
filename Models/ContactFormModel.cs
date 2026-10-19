using System.ComponentModel.DataAnnotations;

namespace WapdaBill.Models;

public class ContactFormModel
{
    [Required(ErrorMessage = "Enter your name.")]
    [StringLength(80, ErrorMessage = "Name must be 80 characters or fewer.")]
    [Display(Name = "Name")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Enter your email.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(160, ErrorMessage = "Email must be 160 characters or fewer.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Enter a message.")]
    [StringLength(2000, MinimumLength = 10, ErrorMessage = "Message must be between 10 and 2000 characters.")]
    [Display(Name = "Message")]
    public string Message { get; set; } = "";

    /// <summary>Honeypot — leave empty. Bots that fill it are ignored.</summary>
    [Display(Name = "Website")]
    public string? Website { get; set; }
}
