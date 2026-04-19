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

        // CanStart remains simple
        public bool CanStart => Status == RequestStatus.Submitted;

        // CanComplete will be set by the controller
        public bool CanComplete { get; set; }
    }
}