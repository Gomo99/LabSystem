using System.ComponentModel.DataAnnotations;

namespace LaboratoryTestRequestManagementSystem.ViewModel
{
    public class PatientRegistrationViewModel
    {
        [Required, StringLength(50)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required, StringLength(50)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Required, StringLength(13)]
        [Display(Name = "South African ID Number")]
        public string SouthAfricanIdNumber { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime DateOfBirth { get; set; }

        [Required, StringLength(20)]
        [Phone]
        [Display(Name = "Cellphone Number")]
        public string CellphoneNumber { get; set; } = string.Empty;

        [Required, EmailAddress]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Home Address")]
        public string HomeAddress { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 8)]
        [DataType(DataType.Password)]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*\d)(?=.*[^\w\s]).{8,}$",
            ErrorMessage = "Password must be at least 8 characters and contain at least one uppercase letter, one number, and one special character.")]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        // --------------- NEW FIELDS for medical history ---------------
        [Display(Name = "Medical Conditions (comma separated)")]
        public string? MedicalConditionsInput { get; set; }

        [Display(Name = "Allergies (comma separated)")]
        public string? AllergiesInput { get; set; }

        [Display(Name = "Medications (comma separated)")]
        public string? MedicationsInput { get; set; }
    }
}