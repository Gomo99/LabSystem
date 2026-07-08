using LaboratoryTestRequestManagementSystem.AppStatus;

namespace LaboratoryTestRequestManagementSystem.ViewModel
{
    public class TestRequestListViewModel
    {
        public int Id { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public DateTime RequestDate { get; set; }
        public Urgency Urgency { get; set; }
        public RequestStatus Status { get; set; }
        public int TestCount { get; set; }
    }
}