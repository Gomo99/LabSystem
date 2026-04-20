using LaboratoryTestRequestManagementSystem.AppStatus;

namespace LaboratoryTestRequestManagementSystem.ViewModel
{
    public class PatientTestRequestListViewModel
    {
        public int Id { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public DateTime RequestDate { get; set; }
        public Urgency Urgency { get; set; }
        public RequestStatus Status { get; set; }
        public int TestCount { get; set; }
        public bool HasResults => Status == RequestStatus.Completed || Status == RequestStatus.ReleasedByDoctor;
    }
}