using System.ComponentModel.DataAnnotations;

namespace LaboratoryTestRequestManagementSystem.ViewModel
{
    public class CaptureResultViewModel
    {
        public int TestRequestId { get; set; }
        public int TestTypeId { get; set; }
        public string TestName { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public string UnitsOfMeasurement { get; set; } = string.Empty;
        public decimal? NormalRangeMin { get; set; }
        public decimal? NormalRangeMax { get; set; }

        // ✅ Patient medical history and clinical notes
        public string? ClinicalNotes { get; set; }
        public List<string> MedicalConditions { get; set; } = new();
        public List<string> Allergies { get; set; } = new();
        public List<string> Medications { get; set; } = new();

        [Required(ErrorMessage = "Result value is required.")]
        [Display(Name = "Result Value")]
        public string ResultValue { get; set; } = string.Empty;

        [Display(Name = "Notes")]
        public string? Notes { get; set; }
    }
}