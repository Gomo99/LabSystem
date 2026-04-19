using System.ComponentModel.DataAnnotations;

namespace LaboratoryTestRequestManagementSystem.ViewModel
{
    public class ReleaseResultsViewModel
    {
        public int RequestId { get; set; }

        [Required(ErrorMessage = "Please enter a note for the patient.")]
        [Display(Name = "Note to Patient")]
        public string Note { get; set; } = string.Empty;

        [Display(Name = "Attach PDF of results")]
        public bool AttachPdf { get; set; } = true;

        [Display(Name = "Ask patient to schedule appointment")]
        public bool RequestAppointment { get; set; }
    }
}