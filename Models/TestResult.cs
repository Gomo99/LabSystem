using LaboratoryTestRequestManagementSystem.AppStatus;
using System.ComponentModel.DataAnnotations;

namespace LaboratoryTestRequestManagementSystem.Models
{
    public class TestResult
    {
        public int Id { get; set; }

        [Required]
        public int TestRequestId { get; set; }
        public TestRequest TestRequest { get; set; } = null!;

        [Required]
        public int TestTypeId { get; set; }
        public TestType TestType { get; set; } = null!;

        public string? ResultValue { get; set; }
        public string? Notes { get; set; }

        public bool IsAbnormal { get; set; }

        public DateTime? CompletedDate { get; set; }
        public DateTime? VerifiedDate { get; set; }

        public int? VerifiedById { get; set; }
        public Employee? VerifiedBy { get; set; }

        public Status Status { get; set; } = Status.Active;
    }
}
