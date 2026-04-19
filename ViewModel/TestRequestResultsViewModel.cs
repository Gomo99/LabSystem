using LaboratoryTestRequestManagementSystem.AppStatus;

namespace LaboratoryTestRequestManagementSystem.ViewModel
{
    public class TestRequestResultsViewModel
    {
        public int RequestId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public DateTime RequestDate { get; set; }
        public List<TestResultViewModel> Results { get; set; } = new();
        public RequestStatus Status { get; set; }
        public string? DoctorNotes { get; set; } // for release note
    }
}
