using LaboratoryTestRequestManagementSystem.AppStatus;

namespace LaboratoryTestRequestManagementSystem.ViewModel
{
    public class DashboardTestItemViewModel
    {
        public int TestRequestId { get; set; }
        public int TestTypeId { get; set; }
        public string RequestNumber => $"REQ-{TestRequestId:D6}";
        public string PatientName { get; set; } = string.Empty;
        public string TestName { get; set; } = string.Empty;
        public Urgency Urgency { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public DateTime? StartDateTime { get; set; }
        public DateTime? ExpectedCompletionTime { get; set; }
        public bool IsOverdue { get; set; }
        public bool IsNearingLimit { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}