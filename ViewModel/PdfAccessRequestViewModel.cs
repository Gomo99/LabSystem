using LaboratoryTestRequestManagementSystem.AppStatus;

namespace LaboratoryTestRequestManagementSystem.ViewModel
{
    public class PdfAccessRequestViewModel
    {
        public int Id { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public DateTime RequestDate { get; set; }
        public int TestRequestId { get; set; }
        public Urgency Urgency { get; set; }
    }
}