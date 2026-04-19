using LaboratoryTestRequestManagementSystem.AppStatus;
using System.ComponentModel.DataAnnotations;

namespace LaboratoryTestRequestManagementSystem.ViewModel
{
    public class LabTechnicianViewModel
    {
        public int? Id { get; set; }

        [Required, StringLength(50)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required, StringLength(50)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Required, StringLength(13)]
        [Display(Name = "South African ID Number")]
        public string SAIDNumber { get; set; } = string.Empty;

        public Status IsActive { get; set; } = Status.Active;


        [Required]
        [Display(Name = "Employee Number")]
        public string EmployeeNumber { get; set; } = string.Empty;

        [Required, EmailAddress]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Phone]
        [Display(Name = "Contact Number")]
        public string ContactNumber { get; set; } = string.Empty;

        [Display(Name = "Assigned Test Types")]
        public List<int> SelectedTestTypeIds { get; set; } = new List<int>();
    }
}
