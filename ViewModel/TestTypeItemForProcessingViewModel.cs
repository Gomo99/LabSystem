// ViewModels/TestTypeItemForProcessingViewModel.cs
using LaboratoryTestRequestManagementSystem.AppStatus;

namespace LaboratoryTestRequestManagementSystem.ViewModel
{
    public class TestTypeItemForProcessingViewModel
    {
        public int TestRequestId { get; set; }
        public int TestTypeId { get; set; }
        public string TestName { get; set; } = null!;
        public string SampleType { get; set; } = null!;
        public RequestStatus Status { get; set; }
        public int? TechnicianId { get; set; }
        public string? TechnicianName { get; set; }
        public DateTime? StartDateTime { get; set; }
        public DateTime? CompletionDateTime { get; set; }
        public bool CanComplete { get; set; }
        public int TurnaroundTimeMinutes { get; set; }
        public DateTime? ExpectedCompletionTime { get; set; }
        public bool IsOverdue { get; set; }
        public int? VerifiedById { get; set; }
        public string? VerifiedByName { get; set; }
        public DateTime? VerifiedDateTime { get; set; }
        public string? VerificationNotes { get; set; }
        public bool CanVerify { get; set; }
        public bool CanReturnForReview { get; set; }
        public bool CanResubmit { get; set; }

        // ✅ NEW
        public bool IsPaused { get; set; }
        public bool CanPause { get; set; }
        public bool CanResume { get; set; }
        public TimeSpan AccumulatedPauseTime { get; set; }
        public string? TechnicianNotes { get; set; }

        // Inside TestTypeItemForProcessingViewModel
        public bool IsDigitallySigned { get; set; }
        public DateTime? SignedAt { get; set; }

        // Computed remaining time (only when InProgress and not paused)
        public string RemainingTimeDisplay
        {
            get
            {
                if (!StartDateTime.HasValue || Status != RequestStatus.InProgress)
                    return "—";

                if (IsPaused)
                    return "Paused";

                var expected = ExpectedCompletionTime.HasValue
                    ? ExpectedCompletionTime.Value
                    : StartDateTime.Value.AddMinutes(TurnaroundTimeMinutes);

                var remaining = expected - DateTime.Now;
                if (remaining.TotalSeconds <= 0)
                    return "Overdue";

                return $"{(int)remaining.TotalHours}h {remaining.Minutes:D2}m";
            }
        }
    }
}