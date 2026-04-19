using LaboratoryTestRequestManagementSystem.AppStatus;
using System.ComponentModel.DataAnnotations;

namespace LaboratoryTestRequestManagementSystem.Models
{
    public class TestRequest
    {
        public int Id { get; set; }

        [Required]
        public int PatientId { get; set; }
        public Patient Patient { get; set; } = null!;

        [Required]
        public int DoctorId { get; set; }
        public Employee Doctor { get; set; } = null!;

        public DateTime RequestDate { get; set; } = DateTime.Now;

        public Urgency Urgency { get; set; } = Urgency.Routine;

        public string? ClinicalNotes { get; set; }

        public RequestStatus RequestStatus { get; set; } = RequestStatus.Submitted;

        public Status RecordStatus { get; set; } = Status.Active; // Soft delete

        // ✅ Cancellation fields
        public DateTime? DateCancelled { get; set; }
        public string? CancellationReason { get; set; }

        // Navigation properties
        public ICollection<TestRequestTestType> TestRequestTestTypes { get; set; } = new List<TestRequestTestType>();
        public ICollection<Sample> Samples { get; set; } = new List<Sample>();
    }
}