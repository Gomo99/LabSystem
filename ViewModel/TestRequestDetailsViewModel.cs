using LaboratoryTestRequestManagementSystem.AppStatus;

namespace LaboratoryTestRequestManagementSystem.ViewModel
{
    public class TestRequestDetailsViewModel
    {
        public int Id { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string DoctorName { get; set; } = string.Empty;
        public DateTime RequestDate { get; set; }
        public Urgency Urgency { get; set; }
        public string? ClinicalNotes { get; set; }
        public RequestStatus Status { get; set; }
        public DateTime? DateCancelled { get; set; }
        public string? CancellationReason { get; set; }
        public List<TestTypeItemViewModel> TestTypes { get; set; } = new();
        public List<SampleItemViewModel> Samples { get; set; } = new();
    }
}