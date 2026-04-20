using LaboratoryTestRequestManagementSystem.AppStatus;

namespace LaboratoryTestRequestManagementSystem.ViewModel
{
    public class PatientTestRequestDetailsViewModel
    {
        public int Id { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public DateTime RequestDate { get; set; }
        public Urgency Urgency { get; set; }
        public string? ClinicalNotes { get; set; }
        public RequestStatus Status { get; set; }
        public bool CanViewResults { get; set; }
        public List<PatientTestResultItemViewModel> TestResults { get; set; } = new();
    }
}
