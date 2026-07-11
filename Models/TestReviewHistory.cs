using System.ComponentModel.DataAnnotations;

namespace LaboratoryTestRequestManagementSystem.Models
{
    public class TestReviewHistory
    {
        public int Id { get; set; }

        public int TestRequestId { get; set; }
        public TestRequest TestRequest { get; set; } = null!;

        public int TestTypeId { get; set; }
        public TestType TestType { get; set; } = null!;

        public int? ReviewerId { get; set; }
        public Employee? Reviewer { get; set; }

        public DateTime ReviewDate { get; set; } = DateTime.Now;

        [Required]
        public string Action { get; set; } = string.Empty; // "Verified", "Returned", "Resubmitted"

        public string? Notes { get; set; }

        public DateTime? ActionDate { get; set; } = DateTime.Now;

        // TestReviewHistory.cs
        public string? VerificationChecklistJson { get; set; }

        public string? ResultChangeJson { get; set; }


    }
}