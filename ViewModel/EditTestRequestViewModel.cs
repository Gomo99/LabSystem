using LaboratoryTestRequestManagementSystem.AppStatus;
using System.ComponentModel.DataAnnotations;

namespace LaboratoryTestRequestManagementSystem.ViewModel
{
    public class EditTestRequestViewModel
    {
        public int Id { get; set; }

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

        // Samples – only editable if status allows
        public List<SampleEntryViewModel> Samples { get; set; } = new();
        public bool CanEditSamples { get; set; } = true;
    }
}