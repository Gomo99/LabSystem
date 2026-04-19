using System.ComponentModel.DataAnnotations;

namespace LaboratoryTestRequestManagementSystem.ViewModel
{
    public class ChangeUsernameViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "New Email Address")]
        public string NewEmail { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [Display(Name = "Confirm New Email")]
        [Compare("NewEmail", ErrorMessage = "Email addresses do not match.")]
        public string ConfirmNewEmail { get; set; } = string.Empty;
    }
}