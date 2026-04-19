using LaboratoryTestRequestManagementSystem.AppStatus;

namespace LaboratoryTestRequestManagementSystem.ViewModel
{
    public class PendingTestRequestViewModel
    {
        public int Id { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string DoctorName { get; set; } = string.Empty;
        public DateTime RequestDate { get; set; }
        public Urgency Urgency { get; set; }
        public int SampleCount { get; set; }
    }
}