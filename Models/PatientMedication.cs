using LaboratoryTestRequestManagementSystem.AppStatus;

namespace LaboratoryTestRequestManagementSystem.Models
{
    public class PatientMedication
    {
        public int PatientId { get; set; }
        public Patient Patient { get; set; } = null!;

        public int MedicationId { get; set; }
        public Medication Medication { get; set; } = null!;

        public Status Status { get; set; } = Status.Active;

    }
}