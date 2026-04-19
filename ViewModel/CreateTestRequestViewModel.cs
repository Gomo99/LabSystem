using LaboratoryTestRequestManagementSystem.AppStatus;
using System.ComponentModel.DataAnnotations;

namespace LaboratoryTestRequestManagementSystem.ViewModel
{
    public class CreateTestRequestViewModel
    {
        [Required]
        [Display(Name = "Patient")]
        public int PatientId { get; set; }

        [Display(Name = "Urgency")]
        public Urgency Urgency { get; set; } = Urgency.Routine;

        [Display(Name = "Clinical Notes")]
        public string? ClinicalNotes { get; set; }

        [Required(ErrorMessage = "Please select at least one test type.")]
        [Display(Name = "Test Types")]
        public List<int> SelectedTestTypeIds { get; set; } = new();

        // Samples – dynamic collection, each with barcode and sample type
        public List<SampleEntryViewModel> Samples { get; set; } = new();
    }

    
}