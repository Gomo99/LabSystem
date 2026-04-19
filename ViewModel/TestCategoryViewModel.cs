using System.ComponentModel.DataAnnotations;

namespace LaboratoryTestRequestManagementSystem.ViewModel
{
    public class TestCategoryViewModel
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        [Display(Name = "Category Name")]
        public string CategoryName { get; set; } = string.Empty;

        [Display(Name = "Description")]
        public string? Description { get; set; }
    }
}
