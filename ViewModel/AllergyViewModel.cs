using System.ComponentModel.DataAnnotations;

namespace LaboratoryTestRequestManagementSystem.ViewModel
{
    public class AllergyViewModel
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        [Display(Name = "Allergy Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Category")]
        public string? Category { get; set; }
    }
}