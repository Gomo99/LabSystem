using System.ComponentModel.DataAnnotations;

namespace LaboratoryTestRequestManagementSystem.ViewModel
{
    public class SampleTypeViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Sample type name is required.")]
        [StringLength(100)]
        public string Name { get; set; }
    }
}
