using System.ComponentModel.DataAnnotations;

namespace LaboratoryTestRequestManagementSystem.ViewModel
{
    public class EditPatientViewModel
    {
        public int Id { get; set; }

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

        [Display(Name = "Medical Conditions (comma separated)")]
        public string MedicalConditionsInput { get; set; } = string.Empty;

        [Display(Name = "Allergies (comma separated)")]
        public string AllergiesInput { get; set; } = string.Empty;

        [Display(Name = "Current Medications (comma separated)")]
        public string MedicationsInput { get; set; } = string.Empty;
    }
}