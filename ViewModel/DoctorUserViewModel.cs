using System.ComponentModel.DataAnnotations;

namespace LaboratoryTestRequestManagementSystem.ViewModel
{
    public class DoctorUserViewModel
    {
        public int? Id { get; set; }

        [Required, StringLength(50)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required, StringLength(50)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Required, StringLength(20)]
        [Display(Name = "HPCSA Number")]
        public string HPCSANumber { get; set; } = string.Empty;

        [Required, EmailAddress]
        [Display(Name = "Email Address (Username)")]
        public string Email { get; set; } = string.Empty;

        [Phone]
        [Display(Name = "Contact Number")]
        public string ContactNumber { get; set; } = string.Empty;
    }
}