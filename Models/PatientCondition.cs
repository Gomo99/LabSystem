using LaboratoryTestRequestManagementSystem.AppStatus;

namespace LaboratoryTestRequestManagementSystem.Models
{
    public class PatientCondition
    {
        public int PatientId { get; set; }
        public Patient Patient { get; set; } = null!;

        public int MedicalConditionId { get; set; }
        public MedicalCondition MedicalCondition { get; set; } = null!;

        public Status Status { get; set; } = Status.Active;

    }
}