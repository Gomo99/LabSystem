using LaboratoryTestRequestManagementSystem.AppStatus;

namespace LaboratoryTestRequestManagementSystem.Models
{
    public class DoctorPatientAccess
    {
        public int Id { get; set; }

        public int PatientId { get; set; }
        public Patient Patient { get; set; } = null!;

        public int DoctorId { get; set; }
        public Employee Doctor { get; set; } = null!; // Assuming Employee with Role.Doctor

        public DateTime GrantedDate { get; set; } = DateTime.Now;

        // Optional: track which test requests are shared (can be expanded)
        public string? SharedTestRequestIds { get; set; } // Comma-separated or JSON

        public Status Status { get; set; } = Status.Active;
    }
}