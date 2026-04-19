using LaboratoryTestRequestManagementSystem.AppStatus;
using System.ComponentModel.DataAnnotations;

namespace LaboratoryTestRequestManagementSystem.Models
{
    public class TestRequestTestType
    {
        public int TestRequestId { get; set; }
        public TestRequest TestRequest { get; set; } = null!;

        public int TestTypeId { get; set; }
        public TestType TestType { get; set; } = null!;

        public RequestStatus RequestStatus { get; set; } = RequestStatus.Submitted;
        public Status RecordStatus { get; set; } = Status.Active;

        // ✅ New fields for processing
        public int? TechnicianId { get; set; }
        public Employee? Technician { get; set; }

        public DateTime? StartDateTime { get; set; }
        public DateTime? CompletionDateTime { get; set; }
    }
}