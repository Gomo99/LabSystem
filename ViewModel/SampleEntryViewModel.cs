using System.ComponentModel.DataAnnotations;

namespace LaboratoryTestRequestManagementSystem.ViewModel
{
    public class SampleEntryViewModel
    {
        [Required]
        public string Barcode { get; set; } = string.Empty;

        [Required]
        public int SampleTypeId { get; set; }
    }
}