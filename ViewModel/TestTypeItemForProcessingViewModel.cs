using LaboratoryTestRequestManagementSystem.AppStatus;

namespace LaboratoryTestRequestManagementSystem.ViewModel
{
    public class TestTypeItemForProcessingViewModel
    {
        public int TestRequestId { get; set; }
        public int TestTypeId { get; set; }
        public string TestName { get; set; } = string.Empty;
        public string SampleType { get; set; } = string.Empty;
        public RequestStatus Status { get; set; }
        public int? TechnicianId { get; set; }
        public string? TechnicianName { get; set; }
        public DateTime? StartDateTime { get; set; }
        public DateTime? CompletionDateTime { get; set; }
        public bool CanStart => Status == RequestStatus.Submitted;
        public bool CanComplete { get; set; }

        // Verification properties
        public int? VerifiedById { get; set; }
        public string? VerifiedByName { get; set; }
        public DateTime? VerifiedDateTime { get; set; }
        public string? VerificationNotes { get; set; }
        public bool CanVerify { get; set; }
        public bool CanReturnForReview { get; set; }
        public bool CanResubmit { get; set; }

        // ✅ Turnaround / Overdue
        public int TurnaroundTimeMinutes { get; set; }
        public DateTime? ExpectedCompletionTime { get; set; }
        public bool IsOverdue { get; set; }
    }
}