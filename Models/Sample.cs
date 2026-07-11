using LaboratoryTestRequestManagementSystem.AppStatus;
using System.ComponentModel.DataAnnotations;

namespace LaboratoryTestRequestManagementSystem.Models
{
    public class Sample
    {
        public int Id { get; set; }
        [Required]
        public string Barcode { get; set; } = null!;

        public int TestRequestId { get; set; }
        public TestRequest TestRequest { get; set; } = null!;

        public int SampleTypeId { get; set; }
        public SampleType SampleType { get; set; } = null!;

        public DateTime? CollectedDate { get; set; }
        public DateTime? ReceivedDate { get; set; }

        public int? ReceivedById { get; set; }
        public Employee? ReceivedBy { get; set; }

        public Status Status { get; set; } = Status.Active;

        // ✅ NEW
        public bool IsDamaged { get; set; }
        public string? RejectionReason { get; set; }
    }
}