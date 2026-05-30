using LaboratoryTestRequestManagementSystem.AppStatus;

namespace LaboratoryTestRequestManagementSystem.Models
{
    public class ReportAccessRequest
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public int DoctorId { get; set; }
        public int TestRequestId { get; set; }          // The specific test request
        public DateTime RequestDate { get; set; } = DateTime.Now;
        public AccessRequestStatus Status { get; set; } = AccessRequestStatus.Pending;
        public DateTime? ResponseDate { get; set; }
        public string? DenyReason { get; set; }

        // Navigation
        public Patient Patient { get; set; } = null!;
        public Employee Doctor { get; set; } = null!;
        public TestRequest TestRequest { get; set; } = null!;
    }
}
