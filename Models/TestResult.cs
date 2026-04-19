namespace LaboratoryTestRequestManagementSystem.Models
{
    using global::LaboratoryTestRequestManagementSystem.AppStatus;
    using System.ComponentModel.DataAnnotations;

    namespace LaboratoryTestRequestManagementSystem.Models
    {
        public class TestResult
        {
            public int Id { get; set; }

            [Required]
            public int TestRequestTestTypeId { get; set; }
            public TestRequestTestType TestRequestTestType { get; set; } = null!;

            public string? ResultValue { get; set; }       // Could be numeric or text
            public string? Notes { get; set; }

            public bool IsAbnormal { get; set; }

            public DateTime? CompletedDate { get; set; }
            public DateTime? VerifiedDate { get; set; }

            public int? VerifiedById { get; set; }
            public Employee? VerifiedBy { get; set; }

            // For soft delete if needed (not required for doctor view)
            public Status Status { get; set; } = Status.Active;
        }
    }
}
